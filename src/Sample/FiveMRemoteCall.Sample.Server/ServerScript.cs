using CitizenFX.Core;
using FiveMRemoteCall.Sample.Server.Remotes;
using FiveMRemoteCall.Server.Helpers;
using FiveMRemoteCall.Server.Services;

namespace FiveMRemoteCall.Sample.Server
{
	public class ServerScript : BaseScript
	{
		public ServerScript()
		{
			LogHelper.LogAction = Debug.WriteLine;

			var remoteCallService = new RemoteCallService(EventHandlers, new []
			{
				new ExampleServerRemote()
			});

			remoteCallService.Start();
		}
	}
}
