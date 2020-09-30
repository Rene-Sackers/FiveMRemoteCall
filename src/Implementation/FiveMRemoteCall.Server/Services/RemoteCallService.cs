using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CitizenFX.Core;
using FiveMRemoteCall.Server.Extensions;
using FiveMRemoteCall.Server.Helpers;
using FiveMRemoteCall.Server.Models;
using FiveMRemoteCall.Shared;

namespace FiveMRemoteCall.Server.Services
{
	public class RemoteCallService
	{
		private static readonly Type TaskType = typeof(Task);

		private readonly EventHandlerDictionary _eventHandlerDictionary;
		private readonly IEnumerable<IRemote> _remotes;
		private readonly Dictionary<string, RemoteInfo> _remotesByAqn = new Dictionary<string, RemoteInfo>();
		private readonly ConcurrentDictionary<Guid, dynamic> _remoteCallCompletionSources = new ConcurrentDictionary<Guid, dynamic>();

		public RemoteCallService(
			EventHandlerDictionary eventHandlerDictionary,
			IEnumerable<IRemote> remotes)
		{
			_eventHandlerDictionary = eventHandlerDictionary;
			_remotes = remotes;
		}

		public void Start()
		{
			LogHelper.Log("Starting remote call service");

			foreach (var remote in _remotes)
			{
				var remoteInfo = new RemoteInfo(remote);
				_remotesByAqn.Add(remote.ResolveAsType.AssemblyQualifiedName, remoteInfo);

				remoteInfo.MethodsByName.Values
					.ToList()
					.ForEach(v => LogHelper.Log($"Added remote method {remote.ResolveAsType.FullName}.{v}"));
			}

			_eventHandlerDictionary[Constants.CallServerEvent] += new Action<Player, string, string, string, ExpandoObject>(CallMethodHandler);
			_eventHandlerDictionary[Constants.CallClientEvent] += new Action<string, ExpandoObject>(RemoteCallCallback);
		}

		public async void CallMethodHandler([FromSource] Player source, string id, string instanceAqn, string method, ExpandoObject parameter)
		{
			LogHelper.Log($"Remove event call recieved from {source.Name} - {id} - {method}");

			var returnValue = await CallLocalMethod(instanceAqn, method, parameter);

			source.TriggerEvent(Constants.CallServerEvent, id, returnValue);
		}

		private async Task<object> CallLocalMethod(string instanceAqn, string method, ExpandoObject parameter)
		{
			if (!_remotesByAqn.TryGetValue(instanceAqn, out var remoteInfo))
			{
				LogHelper.Log($"Could not resolve remote with assembly qualified name {instanceAqn}");
				return null;
			}

			if (!remoteInfo.MethodsByName.TryGetValue(method, out var targetMethod))
			{
				LogHelper.Log($"Could not resolve target method {instanceAqn}.{method}");
				return null;
			}

			var parameterType = parameter != null ? targetMethod.GetParameters().FirstOrDefault()?.ParameterType : null;
			var parameters = parameterType != null
				? new[] { parameter.Cast(parameterType) }
				: new object[0];

			var result = targetMethod.Invoke(remoteInfo.Instance, parameters);
			if (result == null)
				return null;

			if (TaskType.IsAssignableFrom(targetMethod.ReturnType))
			{
				// Task with return type
				if (targetMethod.ReturnType.IsGenericType)
					return await (dynamic)result;

				// Task with no return type
				await (dynamic)result;
				return null;
			}

			// Synchronous method
			return result;
		}

		private void RemoteCallCallback(string id, ExpandoObject parameter)
		{
			var guidId = Guid.Parse(id);
			if (!_remoteCallCompletionSources.TryRemove(guidId, out var callback))
				return;

			LogHelper.Log($"Got remote call callback - {id}");

			var parameterKeyValues = parameter?.ToList();
			var targetType = (Type)callback.TargetType;
			var constructor = targetType
				.GetConstructors()
				.Select(c => new { Constructor = c, Parameters = c.GetParameters() })
				.OrderByDescending(c => c.Parameters.Length)
				.First();

			var parameterValues = new object[constructor.Parameters.Length];
			for (var i = 0; i < constructor.Parameters.Length; i++)
				parameterValues[i] = parameterKeyValues[i].Value;

			var instance = Activator.CreateInstance(targetType, parameterValues);

			callback.CompletionSource.TrySetResult((dynamic)instance);
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

		private Task<TReturn> DoRemoteCall<TReturn>(Player player, MethodCallExpression methodCallExpression)
		{
			var method = methodCallExpression.Method;
			var declaringType = method.DeclaringType;
			var remoteAqn = declaringType.AssemblyQualifiedName;
			var methodName = method.Name;
			var parameter = methodCallExpression.Arguments.FirstOrDefault();
			var parameterValue = parameter == null ? null : Expression.Lambda(parameter).Compile().DynamicInvoke();

			var id = Guid.NewGuid();
			var callback = new RemoteCallCallbackInfo<TReturn>();

			Debug.WriteLine($"Executing remote call {declaringType.FullName}.{methodName} - {id}");

			if (!_remoteCallCompletionSources.TryAdd(id, callback))
				return Task.FromResult<TReturn>(default);

			if (player != null)
				BaseScript.TriggerClientEvent(player, Constants.CallClientEvent, id.ToString(), remoteAqn, methodName, parameterValue);
			else
				BaseScript.TriggerClientEvent(Constants.CallClientEvent, id.ToString(), remoteAqn, methodName, parameterValue);

			return callback.CompletionSource.Task;
		}
	}
}