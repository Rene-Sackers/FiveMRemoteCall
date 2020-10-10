using System;
using CitizenFX.Core;
using FiveMRemoteCall.Sample.Shared.Remotes;

namespace FiveMRemoteCall.Sample.Server.Remotes
{
	public class ExampleServerRemote : IExampleServerRemote
	{
		public Type ResolveAsType { get; } = typeof(IExampleServerRemote);

		public Time GetServerTime(string playerHandle)
		{
			var time = DateTime.Now;

			Debug.WriteLine("Player asking for time: " + playerHandle);

			return new Time(time.Hour, time.Minute, time.Second);
		}

		public void StringParameter(string parameter)
		{
			Debug.WriteLine("String parameter: " + parameter);
		}
	}
}
