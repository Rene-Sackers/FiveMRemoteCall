using FiveMRemoteCall.Shared.Models;

namespace FiveMRemoteCall.Sample.Shared.Remotes
{
	public interface IExampleServerRemote : IRemote
	{
		Time GetServerTime(string playerHandle);

		void StringParameter(string parameter);
	}
}
