using System;
using FiveMRemoteCall.Sample.Shared.Remotes;

namespace FiveMRemoteCall.Sample.Server.Remotes
{
	public class ExampleServerRemote : IExampleServerRemote
	{
		public Type ResolveAsType { get; } = typeof(IExampleServerRemote);

		public Time GetServerTime()
		{
			var time = DateTime.Now;

			return new Time(time.Hour, time.Minute, time.Second);
		}
	}
}
