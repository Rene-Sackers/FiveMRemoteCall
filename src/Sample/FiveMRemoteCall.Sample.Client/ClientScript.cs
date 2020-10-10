using System;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using FiveMRemoteCall.Client.Services;
using FiveMRemoteCall.Sample.Client.Remotes;
using FiveMRemoteCall.Sample.Shared.Remotes;
using FiveMRemoteCall.Shared.Helpers;

namespace FiveMRemoteCall.Sample.Client
{
	public class ClientScript : BaseScript
	{
		private RemoteCallService _remoteCallService;

		[EventHandler("onClientResourceStart")]
		public async void OnResourceStart(string resourceName)
		{
			if (API.GetCurrentResourceName() != resourceName)
				return;

			LogHelper.LogAction = Debug.WriteLine;

			_remoteCallService = new RemoteCallService(EventHandlers, new []{ new ExampleClientRemote() });
			_remoteCallService.Start();


			//await remoteCallService.CallRemoteMethod<IExampleServerRemote>(r => r.StringParameter("test param"));

			API.RegisterCommand("getTime", new Action(GetTimeCommand), false);
		}

		private async void GetTimeCommand()
		{
			var serverTime = await _remoteCallService.CallRemoteMethod<IExampleServerRemote, Time>(r => r.GetServerTime(null));

			TriggerEvent("chat:addMessage", new
			{
				color = new[] { 255, 255, 255 },
				args = new[] { $"Server time: {serverTime.Hours:D2}:{serverTime.Minutes:D2}:{serverTime.Seconds:D2}" }
			});
		}
	}
}
