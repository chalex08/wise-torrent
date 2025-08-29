using System.Collections.Concurrent;
using WiseTorrent.Peers.Interfaces;
using WiseTorrent.Pieces.Interfaces;
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
		public IPieceManager? PieceManager { get; set; }

		public PeerManager(ILogger<PeerManager> logger)
		{
			_logger = logger;
		}

		public async Task HandleTrackerResponse(TorrentSession torrentSession, ILogger<PeerConnector> peerConnectorsLogger, CancellationToken cToken, List<Peer>? newPeers = null)
		{
			if (torrentSession.AllPeers.Count == 0) return;
			
			if (newPeers != null)
			{
				foreach (var peer in newPeers)
				{
					_peerConnectors[peer] = new PeerConnector(peer, torrentSession, peerConnectorsLogger);
				}

				await ConnectToAllPeersAsync(newPeers, cToken);
			}
			else
			{
				_torrentSession = torrentSession;
				foreach (var peer in torrentSession.AllPeers)
				{
					if (!_peerConnectors.ContainsKey(peer))
						_peerConnectors[peer] = new PeerConnector(peer, torrentSession, peerConnectorsLogger);
				}

				await ConnectToAllPeersAsync(torrentSession.AllPeers, cToken);
			}
		}

		private async Task ConnectToAllPeersAsync(List<Peer> peers, CancellationToken cToken)
		{
			var tasks = peers.Take(SessionConfig.MaxSwarmSize - _torrentSession!.ConnectedPeers.Count).Select(peer => ConnectToPeerAsync(peer, cToken)) ?? [];
			await Task.WhenAll(tasks);
		}

		private async Task ConnectToPeerAsync(Peer peer, CancellationToken cToken)
		{
			await _connectSemaphore.WaitAsync(cToken);
			try
			{
				_torrentSession!.OutboundMessageQueues[peer] = new();
				await _peerConnectors[peer].InitiateHandshakeAsync(cToken);
				peer.ResetDecay();
				_torrentSession.ConnectedPeers.Add(peer);
				_torrentSession.OnPeerConnected.NotifyListeners(peer);
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

		public void TryQueueMessage(Peer peer, PeerMessage message)
		{
			if (!_torrentSession!.OutboundMessageQueues[peer].TryEnqueue(message))
			{
				_logger.Error($"Queuing of {message.MessageType} message to {peer.PeerID} failed");
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

		public void QueuePieceRequests(Peer peer, CancellationToken token)
		{
			if (_torrentSession == null || PieceManager == null) return;
			if (_torrentSession.PeerRequestCounts[peer] >= SessionConfig.MaxRequestsPerPeer)
				return;

			var missingPieces = PieceManager.GetMissingPieces();
			var availablePieces = peer.AvailablePieces;
			var requestablePieces = missingPieces.Intersect(availablePieces);

			var sortedPieces = PieceManager
				.GetRarestPieces(requestablePieces)
				.Where(p => _torrentSession.PieceRequestCounts.GetValueOrDefault(p, 0) < SessionConfig.MaxRequestsPerPiece)
				.Take(SessionConfig.MaxActivePieces);

			var pending = _torrentSession.PendingRequests.GetOrAdd(peer, _ => new ConcurrentBag<Block>());
			foreach (var pieceIndex in sortedPieces)
			{
				var blocksRequested = 0;
				int pieceLength = GetPieceLength(pieceIndex);
				var blocks = Piece.SplitPieceToBlocks(pieceIndex, pieceLength);
				foreach (var block in blocks)
				{
					if (token.IsCancellationRequested) break;

					if (_torrentSession.PeerRequestCounts[peer] >= SessionConfig.MaxRequestsPerPeer)
						break;

					if (pending.Contains(block)) continue;
					
					TryQueueMessage(peer, PeerMessage.CreateRequestMessage(block));
					_torrentSession.PieceRequestCounts[pieceIndex]++;
					_torrentSession.PeerRequestCounts[peer]++;
					pending.Add(block);
					blocksRequested++;
				}

				if (blocksRequested > 0)
					_logger.Info($"Queued {blocksRequested} piece requests for missing blocks in Piece: {pieceIndex} (Peer: {peer.PeerID})");
			}
		}

		public int GetPieceLength(int index)
		{
			if (_torrentSession == null) return 0;

			int pieceLengthBytes = (int)_torrentSession.Info.PieceLength.ConvertUnit(ByteUnit.Byte).Size;
			long totalSize = _torrentSession.TotalBytes;
			int pieceCount = (int)Math.Ceiling((double)totalSize / pieceLengthBytes);

			if (index < 0 || index >= pieceCount)
				throw new ArgumentOutOfRangeException(nameof(index), "Invalid piece index.");

			// last piece may be shorter
			if (index == pieceCount - 1)
			{
				int remainder = (int)totalSize % pieceLengthBytes;
				return remainder == 0 ? pieceLengthBytes : remainder;
			}

			return pieceLengthBytes;
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

				peer.Metrics.RefreshRates();
				peer.RarePiecesHeldCount = CalculateRarePiecesCount(peer);
				peer.HasAllPieces = IsSeeder(peer);
				peer.TimeoutCount += IsPeerTimedOut(peer) ? 1 : 0;
			}
		}

		private int CalculateRarePiecesCount(Peer peer)
		{
			return 0;
		}

		private bool IsSeeder(Peer peer)
		{
			return peer.AvailablePieces.Count == _torrentSession?.Info.PieceHashes.Length;
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
