namespace WiseTorrent.Utilities.Types
{
	public class PeerTaskBundle
	{
		public IEnumerable<Task> Tasks { get; set; }
		public CancellationTokenSource CTS { get; set; }

		public PeerTaskBundle(IEnumerable<Task> tasks, CancellationTokenSource cts)
		{
			Tasks = tasks;
			CTS = cts;
		}
	}
}
