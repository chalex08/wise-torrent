namespace WiseTorrent.Utilities.Types
{
	public class TorrentSessionMetricsCollector
	{
		private long _totalDownloadedBytes;
		public long TotalDownloadedBytes => _totalDownloadedBytes;

		private long _totalUploadedBytes;
		public long TotalUploadedBytes => _totalUploadedBytes;

		public void RecordSend(int byteCount)
		{
			Interlocked.Add(ref _totalDownloadedBytes, byteCount);
		}

		public void RecordReceive(int byteCount)
		{
			Interlocked.Add(ref _totalUploadedBytes, byteCount);
		}
	}
}
