using System;

namespace FiveMRemoteCall.Shared.Helpers
{
	public static class LogHelper
	{
		public static Action<string> LogAction { get; set; }

		internal static void Log(string message)
		{
			LogAction?.Invoke($"[{DateTime.Now:T}.{DateTime.Now.TimeOfDay.Milliseconds:D3}] {message}");
		}
	}
}
