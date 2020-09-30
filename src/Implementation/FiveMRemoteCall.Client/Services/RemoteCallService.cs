using System;
using System.Collections.Concurrent;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CitizenFX.Core;
using FiveMRemoteCall.Client.Helpers;
using FiveMRemoteCall.Client.Models;
using FiveMRemoteCall.Shared;

namespace FiveMRemoteCall.Client.Services
{
	public class RemoteCallService
	{
		private readonly EventHandlerDictionary _eventHandlerDictionary;

		private readonly ConcurrentDictionary<Guid, dynamic> _getDataCompletionSources = new ConcurrentDictionary<Guid, dynamic>();

		public RemoteCallService(EventHandlerDictionary eventHandlerDictionary)
		{
			_eventHandlerDictionary = eventHandlerDictionary;
		}

		public void Start()
		{
			_eventHandlerDictionary[Constants.CallServerEvent] += new Action<string, ExpandoObject>(GetDataCallback);
		}

		private void GetDataCallback(string id, ExpandoObject parameter)
		{
			var guidId = Guid.Parse(id);
			if (!_getDataCompletionSources.TryRemove(guidId, out var callback))
				return;

			LogHelper.Log($"Got remote call callback - {id}");

			var parameterKeyValues = parameter?.ToList();
			var targetType = (Type) callback.TargetType;
			var constructor = targetType
				.GetConstructors()
				.Select(c => new {Constructor = c, Parameters = c.GetParameters()})
				.OrderByDescending(c => c.Parameters.Length)
				.First();

			var parameterValues = new object[constructor.Parameters.Length];
			for (var i = 0; i < constructor.Parameters.Length; i++)
				parameterValues[i] = parameterKeyValues[i].Value;

			var instance = Activator.CreateInstance(targetType, parameterValues);

			callback.CompletionSource.TrySetResult((dynamic) instance);
		}

		public Task CallRemoteMethod<TRemote>(Expression<Action<TRemote>> expression) where TRemote : IRemote
		{
			return DoCall<object>((MethodCallExpression)expression.Body);
		}

		public Task<TReturn> CallRemoteMethod<TRemote, TReturn>(Expression<Func<TRemote, TReturn>> expression) where TRemote : IRemote
		{
			return DoCall<TReturn>((MethodCallExpression)expression.Body);
		}

		public Task<TReturn> CallRemoteMethod<TRemote, TReturn>(Expression<Func<TRemote, Task<TReturn>>> expression) where TRemote : IRemote
		{
			return DoCall<TReturn>((MethodCallExpression)expression.Body);
		}

		private Task<TReturn> DoCall<TReturn>(MethodCallExpression methodCallExpression)
		{
			var method = methodCallExpression.Method;
			var declaringType = method.DeclaringType;
			var remoteAqn = declaringType.AssemblyQualifiedName;
			var methodName = method.Name;
			var parameter = methodCallExpression.Arguments.FirstOrDefault();
			var parameterValue = parameter == null ? null : Expression.Lambda(parameter).Compile().DynamicInvoke();

			var id = Guid.NewGuid();
			var callback = new GetDataCallback<TReturn>();

			Debug.WriteLine($"Executing remote call {declaringType.FullName}.{methodName} - {id}");

			if (!_getDataCompletionSources.TryAdd(id, callback))
				return Task.FromResult<TReturn>(default);

			BaseScript.TriggerServerEvent(Constants.CallServerEvent, id.ToString(), remoteAqn, methodName, parameterValue);

			return callback.CompletionSource.Task;
		}
	}
}
