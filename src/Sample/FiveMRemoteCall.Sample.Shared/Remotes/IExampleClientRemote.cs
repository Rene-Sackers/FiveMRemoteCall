using FiveMRemoteCall.Shared.Models;

namespace FiveMRemoteCall.Sample.Shared.Remotes
{
	public interface IExampleClientRemote : IRemote
	{
		Time GetClientTime();

		void StringParameter(string parameter);
	}
}
