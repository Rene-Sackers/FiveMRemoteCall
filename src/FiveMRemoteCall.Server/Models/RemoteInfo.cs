using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FiveMRemoteCall.Shared;

namespace FiveMRemoteCall.Server.Models
{
	internal class RemoteInfo
	{
		public IRemote Instance { get; }

		public IReadOnlyDictionary<string, MethodInfo> MethodsByName { get; }

		public RemoteInfo(IRemote instance)
		{
			Instance = instance;
			MethodsByName = Instance
				.GetType()
				.GetMethods(BindingFlags.Public | BindingFlags.Instance)
				.ToDictionary(m => m.Name, m => m);
		}
	}
}