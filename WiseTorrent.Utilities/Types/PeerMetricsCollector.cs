using System.Runtime.CompilerServices;

namespace WiseTorrent.Utilities.Types
{
	public class PeerMetricsCollector
	{
		private long _downloadedBytes;
		private long _uploadedBytes;
		private int _messagesSent;
		private int _messagesReceived;
		private int _pendingRequests;
		private double _responseTimeSum;
		private int _responseCount;

		private DateTime _lastRateUpdate = DateTime.UtcNow;
		private long _lastDownloadedSnapshot;
		private long _lastUploadedSnapshot;

		public long DownloadRate { get; private set; }
		public long UploadRate { get; private set; }
		public TimeSpan AverageResponseTime =>
			_responseCount == 0 ? TimeSpan.Zero : TimeSpan.FromMilliseconds(_responseTimeSum / _responseCount);

		public void RecordSend(int byteCount)
		{
			Interlocked.Add(ref _downloadedBytes, byteCount);
			Interlocked.Increment(ref _messagesSent);
		}

		public void RecordReceive(int byteCount)
		{
			Interlocked.Add(ref _uploadedBytes, byteCount);
			Interlocked.Increment(ref _messagesReceived);
		}

		public void RecordResponseTime(TimeSpan responseTime)
		{
			Interlocked.Add(ref _responseCount, 1);
			Interlocked.Add(ref Unsafe.As<double, long>(ref _responseTimeSum), (long)responseTime.TotalMilliseconds);
		}

		public void IncrementPendingRequests() => Interlocked.Increment(ref _pendingRequests);

		public void DecrementPendingRequests() => Interlocked.Decrement(ref _pendingRequests);

		public void RefreshRates()
		{
			var now = DateTime.UtcNow;
			var elapsed = (now - _lastRateUpdate).TotalSeconds;
			if (elapsed <= 0) return;

			DownloadRate = (long)((_downloadedBytes - _lastDownloadedSnapshot) / elapsed);
			UploadRate = (long)((_uploadedBytes - _lastUploadedSnapshot) / elapsed);

			_lastDownloadedSnapshot = _downloadedBytes;
			_lastUploadedSnapshot = _uploadedBytes;
			_lastRateUpdate = now;
		}

		public PeerMetricsSnapshot GetSnapshot()
		{
			return new PeerMetricsSnapshot
			{
				DownloadRate = DownloadRate,
				UploadRate = UploadRate,
				AverageResponseTime = AverageResponseTime,
				MessagesSent = _messagesSent,
				MessagesReceived = _messagesReceived,
				PendingRequests = _pendingRequests
			};
		}
	}

}
