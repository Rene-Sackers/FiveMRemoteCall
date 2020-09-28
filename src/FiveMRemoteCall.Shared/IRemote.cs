using System;

namespace FiveMRemoteCall.Shared
{
	public interface IRemote
	{
		Type ResolveAsType { get; }
	}
}
