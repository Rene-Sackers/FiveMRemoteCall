﻿using System;
using CitizenFX.Core;
using FiveMRemoteCall.Sample.Shared.Remotes;

namespace FiveMRemoteCall.Sample.Client.Remotes
{
	public class ExampleClientRemote : IExampleClientRemote
	{
		public Type ResolveAsType { get; } = typeof(IExampleClientRemote);

		public Time GetClientTime()
		{
			var time = DateTime.Now;

			return new Time(time.Hour, time.Minute, time.Second);
		}

		public void StringParameter(string parameter)
		{
			Debug.WriteLine("String parameter: " + parameter);
		}
	}
}
