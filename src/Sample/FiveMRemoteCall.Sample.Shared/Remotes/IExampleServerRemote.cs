using FiveMRemoteCall.Shared;

namespace FiveMRemoteCall.Sample.Shared.Remotes
{
	public interface IExampleServerRemote : IRemote
	{
		Time GetServerTime();
	}
}
