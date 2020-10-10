using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FiveMRemoteCall.Shared.Models
{
	public class RemoteInfo
	{
		public IRemote Instance { get; }

		public IReadOnlyDictionary<string, MethodInfo> MethodsByName { get; }

		public RemoteInfo(IRemote instance)
		{
			Instance = instance;
			MethodsByName = Instance
				.GetType()
				.GetMethods(BindingFlags.Public | BindingFlags.Instance)
				.Where(mi => !mi.IsSpecialName)
				.ToDictionary(m => m.Name, m => m);
		}
	}
}