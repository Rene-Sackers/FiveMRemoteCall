using System;

namespace FiveMRemoteCall.Client.Helpers
{
	public static class LogHelper
	{
		public static Action<string> LogAction { get; set; }

		internal static void Log(string message)
		{
			LogAction?.Invoke(message);
		}
	}
}
