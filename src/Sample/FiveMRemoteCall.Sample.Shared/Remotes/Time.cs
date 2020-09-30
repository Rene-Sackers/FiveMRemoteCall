namespace FiveMRemoteCall.Sample.Shared.Remotes
{
	public class Time
	{
		public int Hours { get; set; }

		public int Minutes { get; set; }

		public int Seconds { get; set; }

		public Time()
		{
		}

		public Time(int hours, int minutes, int seconds)
		{
			Hours = hours;
			Minutes = minutes;
			Seconds = seconds;
		}
	}
}
