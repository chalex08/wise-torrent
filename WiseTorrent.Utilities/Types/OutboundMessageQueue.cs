using System.Collections.Concurrent;

namespace WiseTorrent.Utilities.Types
{
	public class OutboundMessageQueue
	{
		private readonly ConcurrentQueue<PeerMessage> _queue = new();
		private readonly SemaphoreSlim _signal = new(0);

		public OutboundMessageQueue()
		{
		}

		public bool TryEnqueue(PeerMessage message)
		{
			if (_queue.Count >= SessionConfig.MaxOutboundMessageQueueSize)
				return false;

			_queue.Enqueue(message);
			_signal.Release();
			return true;
		}

		public async Task<PeerMessage?> DequeueAsync(CancellationToken token)
		{
			await _signal.WaitAsync(token);

			if (_queue.TryDequeue(out var message))
				return message;

			return null;
		}
	}

}
