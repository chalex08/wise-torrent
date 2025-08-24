namespace WiseTorrent.Utilities.Types
{
	public class PeerMetricsSnapshot
	{
		public long DownloadRate { get; init; }
		public long UploadRate { get; init; }
		public TimeSpan AverageResponseTime { get; init; }
		public int MessagesSent { get; init; }
		public int MessagesReceived { get; init; }
		public int PendingRequests { get; init; }
	}
}
