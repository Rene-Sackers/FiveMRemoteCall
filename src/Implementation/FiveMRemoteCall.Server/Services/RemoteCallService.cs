using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
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

			_eventHandlerDictionary[Constants.CallServerEvent] += new Action<Player, string, string, string, ExpandoObject>(CallMethodCallbackHandler);
		}

		public async void CallMethodCallbackHandler([FromSource] Player source, string id, string instanceAqn, string method, ExpandoObject parameter)
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
	}
}