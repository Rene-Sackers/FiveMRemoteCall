using System;
using System.Threading.Tasks;

namespace FiveMRemoteCall.Client.Models
{
	internal class GetDataCallback<T> 
	{
		public Type TargetType { get; }

		public TaskCompletionSource<T> CompletionSource { get; }

		public GetDataCallback()
		{
			TargetType = typeof(T);
			CompletionSource = new TaskCompletionSource<T>();
		}
	}
}