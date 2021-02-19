using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FiveMRemoteCall.Shared.Helpers;
using FiveMRemoteCall.Shared.Models;

namespace FiveMRemoteCall.Shared.Services
{
	public abstract class RemoteCallServiceBase
	{
		public static string EventPrefix;

		protected static readonly Type TaskType = typeof(Task);

		protected readonly IEnumerable<IRemote> Remotes;
		protected readonly Dictionary<string, RemoteInfo> RemotesByAqn = new Dictionary<string, RemoteInfo>();
		protected readonly ConcurrentDictionary<Guid, dynamic> RemoteCallCompletionSources = new ConcurrentDictionary<Guid, dynamic>();

		internal RemoteCallServiceBase(IEnumerable<IRemote> remotes)
		{
			Remotes = remotes;
		}

		public void Start()
		{
			foreach (var remote in Remotes)
			{
				var remoteInfo = new RemoteInfo(remote);
				RemotesByAqn.Add(remote.ResolveAsType.AssemblyQualifiedName, remoteInfo);

				remoteInfo.MethodsByName.Values
					.ToList()
					.ForEach(v => LogHelper.Log($"Added remote method {remote.ResolveAsType.FullName}.{v.Name}"));
			}

			HookEvents(RemoteCallbackHandler);
		}

		protected abstract void HookEvents(Action<string, object> remoteCallbackHandler);

		private void RemoteCallbackHandler(string id, object parameter)
		{
			var guidId = Guid.Parse(id);
			if (!RemoteCallCompletionSources.TryRemove(guidId, out var callback))
				return;

			LogHelper.Log($"Got remote call callback - {id}");

			var targetType = (Type)callback.TargetType;
			var result = TypeResolveHelper.ResolveType(parameter, targetType);

			callback.CompletionSource.TrySetResult((dynamic)result);
		}

		protected async Task<object> CallLocalMethod(string instanceAqn, string method, List<object> parameters, string playerHandle = null)
		{
			if (!RemotesByAqn.TryGetValue(instanceAqn, out var remoteInfo))
			{
				LogHelper.Log($"Could not resolve remote with assembly qualified name {instanceAqn}");
				return null;
			}

			if (!remoteInfo.MethodsByName.TryGetValue(method, out var targetMethod))
			{
				LogHelper.Log($"Could not resolve target method {instanceAqn}.{method}");
				return null;
			}

			var invokeParameters = TypeResolveHelper.ResolveMethodParameterTypes(parameters, targetMethod, playerHandle);
			if (invokeParameters == null)
				return null;

			var result = targetMethod.Invoke(remoteInfo.Instance, invokeParameters);
			if (result == null)
				return null;

			// Synchronous method
			if (!TaskType.IsAssignableFrom(targetMethod.ReturnType))
				return result;

			// Task with return type
			if (targetMethod.ReturnType.IsGenericType)
				return await (dynamic)result;

			// Task with no return type
			await (dynamic)result;
			return null;
		}
	}
}
