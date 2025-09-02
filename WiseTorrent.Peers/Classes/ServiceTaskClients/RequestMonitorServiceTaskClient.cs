using WiseTorrent.Peers.Interfaces;
using WiseTorrent.Utilities.Interfaces;
using WiseTorrent.Utilities.Types;

namespace WiseTorrent.Peers.Classes.ServiceTaskClients
{
	internal class RequestMonitorServiceTaskClient : IPeerChildServiceTaskClient
	{
		private readonly ILogger<RequestMonitorServiceTaskClient> _logger;
		public TorrentSession? TorrentSession { get; set; }
		public IPeerManager? PeerManager { get; set; }

		public RequestMonitorServiceTaskClient(ILogger<RequestMonitorServiceTaskClient> logger)
		{
			_logger = logger;
		}

		public async Task StartServiceTask(Peer peer, CancellationToken pCToken)
		{
			if (TorrentSession == null || PeerManager == null)
				throw new InvalidOperationException("Dependencies not set");

			while (!pCToken.IsCancellationRequested)
			{
				try
				{
					await Task.Delay(SessionConfig.PieceRequestTimeoutLimitSeconds, pCToken);
					CheckAndRecoverStaleRequests(peer, pCToken);
				}
				catch (OperationCanceledException)
				{
					_logger.Error($"Request monitor cancelled for peer {peer.PeerID ?? peer.IPEndPoint.ToString()}");
					break;
				}
				catch (Exception ex)
				{
					_logger.Error("Peer request monitor service loop encountered error", ex);
				}
			}
		}

		private void CheckAndRecoverStaleRequests(Peer peer, CancellationToken cToken)
		{
			if (!TorrentSession!.PendingRequests.TryGetValue(peer, out var pending))
				return;

			var now = DateTime.UtcNow;
			foreach (var kvp in pending)
			{
				if (kvp.Value < now - SessionConfig.PieceRequestTimeoutLimitSeconds)
				{
					kvp.Key.IsMarkedForRetry = true;
				}
			}

			PeerManager!.QueuePieceRequests(peer, cToken);
		}
	}
}
