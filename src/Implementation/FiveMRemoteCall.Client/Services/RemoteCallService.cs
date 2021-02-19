using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CitizenFX.Core;
using FiveMRemoteCall.Shared.Helpers;
using FiveMRemoteCall.Shared.Models;
using FiveMRemoteCall.Shared.Services;

namespace FiveMRemoteCall.Client.Services
{
	public class RemoteCallService : RemoteCallServiceBase
	{
		private const int RemoteCallCallbackTimeout = 2000;

		private readonly EventHandlerDictionary _eventHandlerDictionary;

		public RemoteCallService(EventHandlerDictionary eventHandlerDictionary, IRemote[] remotes) : base(remotes)
		{
			_eventHandlerDictionary = eventHandlerDictionary;
		}

		protected override void HookEvents(Action<string, object> remoteCallbackHandler)
		{
			LogHelper.Log($"Listen to calls on event {EventPrefix + Constants.CallClientEvent}");

			_eventHandlerDictionary[EventPrefix + Constants.CallClientEvent] += new Action<string, string, string, List<object>>(CallMethodHandler);
			_eventHandlerDictionary[EventPrefix + Constants.CallServerEvent] += remoteCallbackHandler;
		}
		
		private async void CallMethodHandler(string id, string instanceAqn, string method, List<object> parameters)
		{
			LogHelper.Log($"Remote event call recieved - {id} - {method}");

			var returnValue = await CallLocalMethod(instanceAqn, method, parameters);

			BaseScript.TriggerServerEvent(EventPrefix + Constants.CallClientEvent, id, returnValue);
		}

		public Task CallRemoteMethod<TRemote>(Expression<Action<TRemote>> expression) where TRemote : IRemote
		{
			return DoRemoteCall<object>((MethodCallExpression)expression.Body);
		}

		public Task<TReturn> CallRemoteMethod<TRemote, TReturn>(Expression<Func<TRemote, TReturn>> expression) where TRemote : IRemote
		{
			return DoRemoteCall<TReturn>((MethodCallExpression)expression.Body);
		}

		public Task<TReturn> CallRemoteMethod<TRemote, TReturn>(Expression<Func<TRemote, Task<TReturn>>> expression) where TRemote : IRemote
		{
			return DoRemoteCall<TReturn>((MethodCallExpression)expression.Body);
		}

		private async Task<TReturn> DoRemoteCall<TReturn>(MethodCallExpression methodCallExpression)
		{
			var method = methodCallExpression.Method;
			var declaringType = method.DeclaringType;
			var remoteAqn = declaringType.AssemblyQualifiedName;
			var methodName = method.Name;
			var arguments = methodCallExpression.Arguments.Select(a => Expression.Lambda(a).Compile().DynamicInvoke()).ToArray();

			var id = Guid.NewGuid();
			var callback = new RemoteCallCallbackInfo<TReturn>();

			Debug.WriteLine($"Executing remote call {declaringType.FullName}.{methodName} - {id}");

			if (!RemoteCallCompletionSources.TryAdd(id, callback))
				return default;

			BaseScript.TriggerServerEvent(EventPrefix + Constants.CallServerEvent, id.ToString(), remoteAqn, methodName, arguments);

			var timeout = BaseScript.Delay(RemoteCallCallbackTimeout);
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
