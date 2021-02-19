using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CitizenFX.Core;
using FiveMRemoteCall.Shared.Helpers;
using FiveMRemoteCall.Shared.Models;
using FiveMRemoteCall.Shared.Services;

namespace FiveMRemoteCall.Server.Services
{
	public class RemoteCallService : RemoteCallServiceBase
	{
		private const int RemoteCallCallbackTimeout = 2000;

		private readonly EventHandlerDictionary _eventHandlerDictionary;

		public RemoteCallService(EventHandlerDictionary eventHandlerDictionary, IEnumerable<IRemote> remotes) : base(remotes)
		{
			_eventHandlerDictionary = eventHandlerDictionary;
		}

		protected override void HookEvents(Action<string, object> remoteCallbackHandler)
		{
			LogHelper.Log($"Listen to calls on event {EventPrefix + Constants.CallServerEvent}");

			_eventHandlerDictionary[EventPrefix + Constants.CallServerEvent] += new Action<Player, string, string, string, List<object>>(CallMethodHandler);
			_eventHandlerDictionary[EventPrefix + Constants.CallClientEvent] += remoteCallbackHandler;
		}

		private async void CallMethodHandler([FromSource] Player source, string id, string instanceAqn, string method, List<object> parameters)
		{
			LogHelper.Log($"Remote event call recieved from {source.Name} - {id} - {method}");

			var returnValue = await CallLocalMethod(instanceAqn, method, parameters, source.Handle);

			source.TriggerEvent(EventPrefix + Constants.CallServerEvent, id, returnValue);
		}

		public Task CallRemoteMethodAllClients<TRemote>(Expression<Action<TRemote>> expression) where TRemote : IRemote
		{
			return DoRemoteCall<object>(null, (MethodCallExpression)expression.Body);
		}

		public Task CallRemoteMethod<TRemote>(Player player, Expression<Action<TRemote>> expression) where TRemote : IRemote
		{
			return DoRemoteCall<object>(player, (MethodCallExpression)expression.Body);
		}

		public Task<TReturn> CallRemoteMethod<TRemote, TReturn>(Player player, Expression<Func<TRemote, TReturn>> expression) where TRemote : IRemote
		{
			return DoRemoteCall<TReturn>(player, (MethodCallExpression)expression.Body);
		}

		public Task<TReturn> CallRemoteMethod<TRemote, TReturn>(Player player, Expression<Func<TRemote, Task<TReturn>>> expression) where TRemote : IRemote
		{
			return DoRemoteCall<TReturn>(player, (MethodCallExpression)expression.Body);
		}

		private async Task<TReturn> DoRemoteCall<TReturn>(Player player, MethodCallExpression methodCallExpression)
		{
			var method = methodCallExpression.Method;
			var declaringType = method.DeclaringType;
			var remoteAqn = declaringType.AssemblyQualifiedName;
			var methodName = method.Name;
			var arguments = methodCallExpression.Arguments.Select(a => Expression.Lambda(a).Compile().DynamicInvoke()).ToArray();

			var id = Guid.NewGuid();
			var callback = new RemoteCallCallbackInfo<TReturn>();

			LogHelper.Log($"Executing remote call {declaringType.FullName}.{methodName} - {id}");

			if (!RemoteCallCompletionSources.TryAdd(id, callback))
				return default;

			if (player != null)
				BaseScript.TriggerClientEvent(player, EventPrefix + Constants.CallClientEvent, id.ToString(), remoteAqn, methodName, arguments);
			else
				BaseScript.TriggerClientEvent(EventPrefix + Constants.CallClientEvent, id.ToString(), remoteAqn, methodName, arguments);

			var timeout = Task.Delay(RemoteCallCallbackTimeout);
			var completedTask = await Task.WhenAny(timeout, callback.CompletionSource.Task);

			if (completedTask == timeout)
			{
				LogHelper.Log($"Remote call {id} dit not complete within timeout.");
				return default;
			}

			return callback.CompletionSource.Task.Result;
		}
	}
}