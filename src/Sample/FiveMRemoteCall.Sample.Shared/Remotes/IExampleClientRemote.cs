using FiveMRemoteCall.Shared;

namespace FiveMRemoteCall.Sample.Shared.Remotes
{
	public interface IExampleClientRemote : IRemote
	{
		Time GetClientTime();
	}
}
