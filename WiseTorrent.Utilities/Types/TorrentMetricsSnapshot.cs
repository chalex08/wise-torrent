namespace WiseTorrent.Utilities.Types
{
	public class TorrentMetricsSnapshot
	{
		public long DownloadRate { get; init; }
		public long UploadRate { get; init; }
		public int MessagesSent { get; init; }
		public int MessagesReceived { get; init; }
	}
}
