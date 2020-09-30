using CitizenFX.Core;
using CitizenFX.Core.Native;
using FiveMRemoteCall.Client.Helpers;
using FiveMRemoteCall.Client.Services;
using FiveMRemoteCall.Sample.Shared.Remotes;

namespace FiveMRemoteCall.Sample.Client
{
	public class ClientScript : BaseScript
	{
		[EventHandler("onClientResourceStart")]
		public async void OnResourceStart(string resourceName)
		{
			if (API.GetCurrentResourceName() != resourceName)
				return;

			LogHelper.LogAction = Debug.WriteLine;

			var remoteCallService = new RemoteCallService(EventHandlers);
			remoteCallService.Start();

			var serverTime = await remoteCallService.CallRemoteMethod<IExampleServerRemote, Time>(r => r.GetServerTime());
			
			TriggerEvent("chat:addMessage", new
			{
				color = new[] { 255, 255, 255 },
				args = new[] { $"Server time: {serverTime.Hours}:{serverTime.Minutes}:{serverTime.Seconds}" }
			});
		}
	}
}
