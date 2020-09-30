using System;
using System.Threading.Tasks;

namespace FiveMRemoteCall.Client.Models
{
	internal class RemoteCallCallbackInfo<T> 
	{
		public Type TargetType { get; }

		public TaskCompletionSource<T> CompletionSource { get; }

		public RemoteCallCallbackInfo()
		{
			TargetType = typeof(T);
			CompletionSource = new TaskCompletionSource<T>();
		}
	}
}