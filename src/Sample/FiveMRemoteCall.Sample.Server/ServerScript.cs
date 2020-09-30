using System.Threading.Tasks;
using CitizenFX.Core;
using FiveMRemoteCall.Sample.Server.Remotes;
using FiveMRemoteCall.Sample.Shared.Remotes;
using FiveMRemoteCall.Server.Helpers;
using FiveMRemoteCall.Server.Services;

namespace FiveMRemoteCall.Sample.Server
{
	public class ServerScript : BaseScript
	{
		private readonly RemoteCallService _remoteCallService;

		public ServerScript()
		{
			LogHelper.LogAction = Debug.WriteLine;

			_remoteCallService = new RemoteCallService(EventHandlers, new []
			{
				new ExampleServerRemote()
			});

			_remoteCallService.Start();

			CallClient();
		}

		private async void CallClient()
		{
			await Task.Delay(5000);

			foreach (var player in Players)
			{
				var clientTime = await _remoteCallService.CallRemoteMethod<IExampleClientRemote, Time>(player, r => r.GetClientTime());

				Debug.WriteLine($"Player {player.Name} time: {clientTime.Hours:D2}:{clientTime.Minutes:D2}:{clientTime.Seconds:D2}");
			}
		}
	}
}
