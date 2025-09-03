namespace WiseTorrent.Utilities.Types
{
	public class TorrentSessionMetricsCollector
	{
		private long _totalDownloadedBytes;
		public long TotalDownloadedBytes => _totalDownloadedBytes;

		private long _totalUploadedBytes;
		public long TotalUploadedBytes => _totalUploadedBytes;

		private int _messagesSent;
		private int _messagesReceived;

		private DateTime _lastRateUpdate = DateTime.UtcNow;
		private long _lastDownloadedSnapshot;
		private long _lastUploadedSnapshot;

		public long DownloadRate { get; private set; }
		public long UploadRate { get; private set; }

		public void RecordSend(int byteCount)
		{
			Interlocked.Add(ref _totalUploadedBytes, byteCount);
			Interlocked.Increment(ref _messagesSent);
		}

		public void RecordReceive(int byteCount)
		{
			Interlocked.Add(ref _totalDownloadedBytes, byteCount);
			Interlocked.Increment(ref _messagesReceived);
		}

		public void RefreshRates()
		{
			var now = DateTime.UtcNow;
			var elapsed = (now - _lastRateUpdate).TotalSeconds;
			if (elapsed <= 0) return;

			DownloadRate = (long)((_totalDownloadedBytes - _lastDownloadedSnapshot) / elapsed);
			UploadRate = (long)((_totalUploadedBytes - _lastUploadedSnapshot) / elapsed);

			_lastDownloadedSnapshot = _totalDownloadedBytes;
			_lastUploadedSnapshot = _totalUploadedBytes;
			_lastRateUpdate = now;
		}

		public TorrentMetricsSnapshot GetSnapshot()
		{
			return new TorrentMetricsSnapshot
			{
				DownloadRate = DownloadRate,
				UploadRate = UploadRate,
				MessagesSent = _messagesSent,
				MessagesReceived = _messagesReceived
			};
		}
	}
}
