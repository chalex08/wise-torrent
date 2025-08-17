namespace WiseTorrent.Peers.Classes
{
	public class PeerTaskBundle
	{
		public Task ReceiveTask { get; set; }
		public Task SendTask { get; set; }
		public Task KeepAliveTask { get; set; }
		public Task UpdateStateTask { get; set; }
		public CancellationTokenSource CTS { get; set; }

		public PeerTaskBundle(Task receiveTask, Task sendTask, Task keepAliveTask, Task updateStateTask, CancellationTokenSource cts)
		{
			ReceiveTask = receiveTask;
			SendTask = sendTask;
			KeepAliveTask = keepAliveTask;
			UpdateStateTask = updateStateTask;
			CTS = cts;
		}
	}
}
