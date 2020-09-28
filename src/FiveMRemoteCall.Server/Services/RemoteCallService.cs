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
			LogHelper.Log("adding event handler");

			foreach (var remote in _remotes)
			{
				_remotesByAqn.Add(remote.ResolveAsType.AssemblyQualifiedName, new RemoteInfo(remote));
				LogHelper.Log($"Added remote {remote.ResolveAsType.Name}");
			}

			_eventHandlerDictionary[Constants.CallRemoteEvent] += new Action<Player, string, string, string, ExpandoObject>(CallMethodCallbackHandler);
		}

		public async void CallMethodCallbackHandler([FromSource] Player source, string id, string instanceAqn, string method, ExpandoObject parameter)
		{
			LogHelper.Log($"Event call recieved from {source.Name} - {id}");

			var returnValue = await CallLocalMethod(instanceAqn, method, parameter);

			source.TriggerEvent(Constants.CallRemoteEvent, id, returnValue);
		}

		private async Task<object> CallLocalMethod(string instanceAqn, string method, ExpandoObject parameter)
		{
			if (!_remotesByAqn.TryGetValue(instanceAqn, out var remoteInfo))
			{
				LogHelper.Log("Could not resolve remote with aqn " + instanceAqn);
				return null;
			}

			if (!remoteInfo.MethodsByName.TryGetValue(method, out var targetMethod))
			{
				LogHelper.Log($"Could not resolve target method {method}");
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
				if (targetMethod.ReturnType.IsGenericType)
					return await (dynamic)result;

				await (dynamic)result;
				return null;
			}

			return result;
		}
	}
}