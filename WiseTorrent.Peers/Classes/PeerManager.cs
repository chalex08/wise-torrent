using WiseTorrent.Peers.Interfaces;
using WiseTorrent.Utilities.Interfaces;
using WiseTorrent.Utilities.Types;

namespace WiseTorrent.Peers.Classes
{
	internal class PeerManager : IPeerManager
	{
		private readonly ILogger<PeerManager> _logger;
		private TorrentSession? _torrentSession;
		private readonly Dictionary<Peer, PeerConnector> _peerConnectors = new();
		private readonly Dictionary<Peer, DateTime> _disconnectTimestamps = new();
		private readonly SemaphoreSlim _connectSemaphore = new(SessionConfig.MaxPeerConnectionThreads);

		public PeerManager(ILogger<PeerManager> logger)
		{
			_logger = logger;
		}

		public async Task HandleTrackerResponse(TorrentSession torrentSession, ILogger<PeerConnector> peerConnectorsLogger, CancellationToken cToken)
		{
			if (torrentSession.AllPeers.Count == 0) return;

			_torrentSession = torrentSession;
			foreach (var peer in torrentSession.AllPeers)
			{
				if (!_peerConnectors.ContainsKey(peer))
					_peerConnectors[peer] = new PeerConnector(peer, torrentSession, peerConnectorsLogger);
			}

			await ConnectToAllPeersAsync(cToken);
		}

		public async Task ConnectToAllPeersAsync(CancellationToken cToken)
		{
			var tasks = _torrentSession?.AllPeers.Take(SessionConfig.MaxSwarmSize).Select(peer => ConnectToPeerAsync(peer, cToken)) ?? [];
			await Task.WhenAll(tasks);
		}

		public async Task ConnectToPeerAsync(Peer peer, CancellationToken cToken)
		{
			await _connectSemaphore.WaitAsync(cToken);
			try
			{
				await _peerConnectors[peer].ConnectAsync(cToken);
				peer.ResetDecay();
				_torrentSession?.ConnectedPeers.Add(peer);
				_torrentSession?.OnPeerConnected.NotifyListeners(peer);
			}
			catch (OperationCanceledException)
			{
				_logger.Info($"Connection to {peer.PeerID} was cancelled.");
			}
			finally
			{
				_connectSemaphore.Release();
			}

		}

		public async Task<bool> SendPeerMessageAsync(Peer peer, byte[] data, CancellationToken cToken)
		{
			try
			{
				return await _peerConnectors[peer].TrySendPeerMessageAsync(data, cToken);
			}
			catch (OperationCanceledException)
			{
				_logger.Info($"Sending message to {peer.PeerID} was cancelled.");
				return false;
			}
		}

		public async Task<byte[]> ReceivePeerMessageAsync(Peer peer, CancellationToken cToken)
		{
			try
			{
				return await _peerConnectors[peer].TryReceivePeerMessageAsync(cToken);
			}
			catch (OperationCanceledException)
			{
				_logger.Info($"Receiving message from {peer.PeerID} was cancelled.");
				return [];
			}
		}

		public async Task DisconnectAllPeersAsync(CancellationToken cToken)
		{
			if (_torrentSession == null) return;

			foreach (var peer in _torrentSession.ConnectedPeers)
			{
				await DisconnectPeerAsync(peer, cToken);
			}
		}

		public async Task DisconnectPeerAsync(Peer peer, CancellationToken cToken)
		{
			if (_torrentSession == null) return;

			peer.IsConnected = false;
			peer.LastActive = DateTime.UtcNow;
			await _peerConnectors[peer].DisconnectAsync(cToken);
			_disconnectTimestamps[peer] = DateTime.UtcNow;
			_torrentSession.ConnectedPeers.Remove(peer);
			_torrentSession.OnPeerDisconnected.NotifyListeners(peer);
		}

		public void UpdatePeerStatesAsync(CancellationToken cToken)
		{
			if (_torrentSession == null) return;

			foreach (var peer in _torrentSession.AllPeers)
			{
				if (!_torrentSession.ConnectedPeers.Contains(peer)) peer.DecayScore();

				peer.DownloadRate = CalculateDownloadRate(peer);
				peer.UploadRate = CalculateUploadRate(peer);
				peer.PendingRequestCount = CalculatePendingRequests(peer);
				peer.AverageResponseTime = CalculateAverageResponseTime(peer);
				peer.RarePiecesHeldCount = CalculateRarePiecesCount(peer);
				peer.HasAllPieces = IsSeeder(peer);
				peer.TimeoutCount += IsPeerTimedOut(peer) ? 1 : 0;
			}
		}

		private long CalculateDownloadRate(Peer peer)
		{
			return 0;
		}

		private long CalculateUploadRate(Peer peer)
		{
			return 0;
		}

		private int CalculatePendingRequests(Peer peer)
		{
			return 0;
		}

		private TimeSpan CalculateAverageResponseTime(Peer peer)
		{
			return TimeSpan.FromSeconds(0);
		}

		private int CalculateRarePiecesCount(Peer peer)
		{
			return 0;
		}

		private bool IsSeeder(Peer peer)
		{
			return false;
		}

		private bool IsPeerTimedOut(Peer peer)
		{
			TimeSpan idleTime = DateTime.UtcNow - peer.LastReceived;
			return idleTime.TotalSeconds > SessionConfig.PeerTimeoutSeconds;
		}

		public async Task UpdatePeerSelectionAsync(CancellationToken cToken)
		{
			if (_torrentSession == null) return;

			foreach (var peer in _torrentSession.ConnectedPeers)
			{
				var score = peer.CalculatePeerScore();
				if (score >= 80)
				{
					UnchokePeer(peer);
					_logger.Info($"Peer {peer.PeerID} unchoked (Performance score: {score})");
				}
				else if (score >= 40)
				{
					MonitorPeer(peer);
					_logger.Info($"Peer {peer.PeerID} choked, but monitored (Performance score: {score})");
				}
				else
				{
					await RotatePeer(peer, cToken);
					_logger.Info($"Peer {peer.PeerID} replaced (Performance score: {score})");
				}
			}
		}

		private void UnchokePeer(Peer peer)
		{
			peer.IsChoked = false;
			peer.IsInterested = true;
		}

		private void MonitorPeer(Peer peer)
		{
			peer.IsChoked = true;
			peer.IsInterested = false;
		}

		private async Task RotatePeer(Peer peer, CancellationToken cToken)
		{
			await DisconnectPeerAsync(peer, cToken);
			_logger.Info($"Peer {peer.PeerID} disconnected due to inactivity");

			await ConnectNextBestPeer(cToken);
		}

		private async Task ConnectNextBestPeer(CancellationToken cToken)
		{
			var replacementPeer = _torrentSession?.AllPeers
				.Except(_torrentSession.ConnectedPeers)
				.OrderByDescending(p => p.CalculatePeerScore())
				.FirstOrDefault(p => !IsInCooldown(p) && p.CalculatePeerScore() > 30);

			if (replacementPeer != null)
			{
				await ConnectToPeerAsync(replacementPeer, cToken);
				_logger.Info($"Peer {replacementPeer.PeerID} connected to replace under performing peer");
			}
		}

		private bool IsInCooldown(Peer peer)
		{
			var cooldown = SessionConfig.PeerReconnectCooldownSeconds;
			return _disconnectTimestamps.TryGetValue(peer, out var lastDisconnect) && DateTime.UtcNow - lastDisconnect < cooldown;
		}
	}
}
