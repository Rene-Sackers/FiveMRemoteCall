using System;

namespace FiveMRemoteCall.Shared.Models
{
	public interface IRemote
	{
		Type ResolveAsType { get; }
	}
}
