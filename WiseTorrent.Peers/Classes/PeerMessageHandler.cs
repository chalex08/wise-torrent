using System.Collections.Concurrent;
using System.Net;
using WiseTorrent.Peers.Interfaces;
using WiseTorrent.Pieces.Interfaces;
using WiseTorrent.Utilities.Interfaces;
using WiseTorrent.Utilities.Types;

namespace WiseTorrent.Peers.Classes
{
	public class PeerMessageHandler
	{
		private readonly ILogger<PeerMessageHandler> _logger;
		private readonly TorrentSession _torrentSession;
		private readonly IPeerManager _peerManager;
		private readonly IPieceManager _pieceManager;

		public PeerMessageHandler(ILogger<PeerMessageHandler> logger, TorrentSession torrentSession, IPeerManager peerManager, IPieceManager pieceManager)
		{
			_logger = logger;
			_torrentSession = torrentSession;
			_peerManager = peerManager;
			_pieceManager = pieceManager;
		}

		public void HandleMessage(Peer peer, PeerMessage message, CancellationToken token)
		{
			if (message.HandshakeMessage != null)
			{
				_logger.Info($"(Peer: {peer.PeerID ?? peer.IPEndPoint.ToString()}) Received handshake from peer");
				if (TryHandleHandshake(peer, message))
				{
					SendBitfield(peer, message);
					SendInterested(peer);
				}
			}
			else
			{
				switch (message.MessageType)
				{
					case PeerMessageType.Bitfield:
						_logger.Info($"(Peer: {peer.PeerID ?? peer.IPEndPoint.ToString()}) Received bitfield from peer");
						HandleBitfield(peer, message);
						break;

					case PeerMessageType.Have:
						HandleHave(peer, message);
						break;

					case PeerMessageType.Choke:
						peer.IsChoked = true;
						_logger.Info($"(Peer: {peer.PeerID ?? peer.IPEndPoint.ToString()}) Choked by peer");
						break;

					case PeerMessageType.Unchoke:
						peer.IsChoked = false;
						_logger.Info($"(Peer: {peer.PeerID ?? peer.IPEndPoint.ToString()}) Unchoked by peer. Queuing piece requests");
						_peerManager.QueuePieceRequests(peer, token);
						break;

					case PeerMessageType.Interested:
						HandleInterested(peer, token);
						_logger.Info($"(Peer: {peer.PeerID ?? peer.IPEndPoint.ToString()}) Set to interested by peer");
						break;

					case PeerMessageType.NotInterested:
						peer.IsInterested = false;
						_logger.Info($"(Peer: {peer.PeerID ?? peer.IPEndPoint.ToString()}) Set to not interested by peer");
						break;

					case PeerMessageType.Request:
						HandleRequest(peer, message, token);
						break;

					case PeerMessageType.Piece:
						HandlePiece(peer, message, token);
						break;

					case PeerMessageType.KeepAlive:
						peer.LastActive = DateTime.UtcNow;
						_logger.Info($"(Peer: {peer.PeerID ?? peer.IPEndPoint.ToString()}) Received keep-alive from peer");
						break;
					case PeerMessageType.Cancel:
						HandleCancel(peer, message);
						break;
					default:
						_logger.Warn($"Unknown message type from {peer.PeerID ?? peer.IPEndPoint.ToString()}: {message.MessageType}");
						break;
				}
			}

			peer.FollowsMessageOrder = IsMessageInOrder(peer, message);
			peer.LastReceived = DateTime.UtcNow;
		}

		private bool TryHandleHandshake(Peer peer, PeerMessage message)
		{
			if (message.HandshakeMessage == null) return false;

			if (message.HandshakeMessage == null || !message.HandshakeMessage.InfoHash.SequenceEqual(_torrentSession.InfoHash))
			{
				_logger.Info($"Handshake unsuccessful (PeerID: {peer.PeerID ?? peer.IPEndPoint.ToString()})");
				return false;
			}

			peer.PeerID = message.HandshakeMessage.PeerId;
			peer.IsConnected = true;
			peer.HandshakeCompleted = true;
			peer.ProtocolStage = PeerProtocolStage.AwaitingBitfield;
			_torrentSession.ConnectedPeers.Add(peer);
			_torrentSession.AwaitingHandshakePeers.Remove(peer);

			_torrentSession.OnPeerConnected.NotifyListeners(peer);
			_logger.Info($"Handshake successful. Peer now connected (PeerID: {peer.PeerID ?? peer.IPEndPoint.ToString()})");
			return true;
		}

		private void SendBitfield(Peer peer, PeerMessage message)
		{
			var bitfieldPayload = peer.BuildBitfield(_torrentSession.Info.PieceHashes.Length);
			var bitfieldMessage = new PeerMessage(PeerMessageType.Bitfield, bitfieldPayload);
			_peerManager.TryQueueMessage(peer, bitfieldMessage);
			_logger.Info($"(Peer: {peer.PeerID ?? peer.IPEndPoint.ToString()}) Queued bitfield message to peer");
		}

		private bool IsMessageInOrder(Peer peer, PeerMessage message)
		{
			return peer.ProtocolStage switch
			{
				PeerProtocolStage.AwaitingHandshake => message.HandshakeMessage != null,
				PeerProtocolStage.AwaitingBitfield => message.MessageType == PeerMessageType.Bitfield,
				PeerProtocolStage.AwaitingHaveOrRequest => message.MessageType is PeerMessageType.Have
					or PeerMessageType.Request,
				PeerProtocolStage.AwaitingPiece => message.MessageType == PeerMessageType.Piece,
				PeerProtocolStage.Established => true,
				_ => false
			};
		}

		private void HandleBitfield(Peer peer, PeerMessage message)
		{
			peer.AvailablePieces.Clear();
			for (int i = 0; i < _torrentSession.Info.PieceHashes.Length; i++)
			{
				int byteIndex = i / 8;
				int bitIndex = 7 - (i % 8); // MSB first

				if (byteIndex < message.Payload.Length && (message.Payload[byteIndex] & (1 << bitIndex)) != 0)
					peer.AvailablePieces.Add(i);
			}

			peer.BitfieldReceived = true;
			peer.ProtocolStage = PeerProtocolStage.AwaitingHaveOrRequest;
			_pieceManager.UpdatePieceRarityFromPeer(peer.AvailablePieces);
		}

		private void HandleHave(Peer peer, PeerMessage message)
		{
			if (message.Payload.Length != 4)
			{
				_logger.Warn($"Invalid HAVE message length from peer {peer.PeerID ?? peer.IPEndPoint.ToString()}");
				return;
			}

			int pieceIndex = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(message.Payload, 0));
			if (pieceIndex < 0 || pieceIndex >= _torrentSession.Info.PieceHashes.Length)
			{
				_logger.Warn($"Received HAVE for out-of-range piece {pieceIndex} from peer {peer.PeerID ?? peer.IPEndPoint.ToString()}");
				return;
			}

			peer.AvailablePieces.Add(pieceIndex);
			peer.HasAllPieces = peer.AvailablePieces.Count == _torrentSession.Info.PieceHashes.Length;
			peer.LastActive = DateTime.UtcNow;
			peer.LastReceived = DateTime.UtcNow;
			_pieceManager.UpdatePieceRarityFromPeer(peer.AvailablePieces);
			if (peer.ProtocolStage == PeerProtocolStage.AwaitingHaveOrRequest)
				peer.ProtocolStage = PeerProtocolStage.AwaitingPiece;
			_logger.Info($"(Peer: {peer.PeerID ?? peer.IPEndPoint.ToString()}) Received have message from peer, about Piece: {pieceIndex}");

			if (!_pieceManager.HasPiece(pieceIndex))
			{
				SendInterested(peer);
				_logger.Info($"(Peer: {peer.PeerID ?? peer.IPEndPoint.ToString()}) has piece we want, about Piece: {pieceIndex}");
			}
		}

		private void HandleInterested(Peer peer, CancellationToken cToken)
		{
			peer.IsInterested = true;
			_peerManager.HandlePeerPerformanceScore(peer, peer.CalculatePeerScore(), cToken);
		}

		private void SendInterested(Peer peer)
		{
			var interestedMessage = new PeerMessage(PeerMessageType.Interested);
			_peerManager.TryQueueMessage(peer, interestedMessage);
			peer.IsInterested = true;
			_logger.Info($"(Peer: {peer.PeerID ?? peer.IPEndPoint.ToString()}) Queued interested message to peer");
		}

		private void HandleRequest(Peer peer, PeerMessage message, CancellationToken cToken)
		{
			var parsedBlock = Block.ParseRequestMessage(message.ToBytes());
			if (parsedBlock == null || !IsValidRequest(parsedBlock)) return;

			_torrentSession.OnBlockRequestReceived.NotifyListeners((peer, parsedBlock));
			_torrentSession.OnBlockReadFromDisk.Subscribe(pb =>
			{
				if (pb.Item1 == peer)
				{
					_torrentSession.Metrics.RecordSend(pb.Item2.Length);
					peer.Metrics.RecordReceive(pb.Item2.Length);
					_peerManager.TryQueueMessage(peer, PeerMessage.CreatePieceMessage(pb.Item2));
				}
			});
			if (peer.ProtocolStage == PeerProtocolStage.AwaitingHaveOrRequest)
				peer.ProtocolStage = PeerProtocolStage.AwaitingPiece;
			_logger.Info($"(Peer: {peer.PeerID ?? peer.IPEndPoint.ToString()}) Received request from peer, about (Piece Index, Block Offset): ({parsedBlock.PieceIndex}, {parsedBlock.Offset})");
		}

		bool IsValidRequest(Block req)
		{
			if (req.PieceIndex < 0 || req.PieceIndex >= _torrentSession.Info.PieceHashes.Length)
				return false;

			int pieceLength = _peerManager.GetPieceLength(req.PieceIndex);
			if (req.Offset < 0 || req.Offset + req.Length > pieceLength)
				return false;

			return true;
		}

		private void HandlePiece(Peer peer, PeerMessage message, CancellationToken cToken)
		{
			var parsedBlock = Block.ParsePieceMessage(message.ToBytes());
			if (parsedBlock == null || !IsExpectedBlock(peer, parsedBlock) || !IsValidBlock(parsedBlock))
				return;

			if (peer.ProtocolStage == PeerProtocolStage.AwaitingPiece)
				peer.ProtocolStage = PeerProtocolStage.Established;

			var piece = _torrentSession.Pieces.FirstOrDefault(p => p.Index == parsedBlock.PieceIndex);
			if (piece == null) return;

			var block = piece.Blocks.FirstOrDefault(b => b.Offset == parsedBlock.Offset);
			if (block == null || block.Data != null) return;

			block.Data = parsedBlock.Data;

			if (_torrentSession.PendingRequests.TryGetValue(peer, out var pending))
			{
				// remove the block from pending requests
				pending.TryRemove(block, out var requestTime);
				peer.Metrics.RecordResponseTime(DateTime.UtcNow - requestTime);
				peer.Metrics.DecrementPendingRequests();
			}

			_torrentSession.PieceRequestCounts.AddOrUpdate(block.PieceIndex, 0, (k, v) => v > 0 ? v - 1 : 0);
			_torrentSession.PeerRequestCounts.AddOrUpdate(peer, 0, (k, v) => v > 0 ? v - 1 : 0);

			if (piece.IsBlockValid(block))
			{
				_torrentSession.OnBlockReceived.NotifyListeners(block);
				peer.Metrics.RecordSend(block.Length);
			}

			_logger.Info($"(Peer: {peer.PeerID ?? peer.IPEndPoint.ToString()}) Received block (Piece Index: {block.PieceIndex}, Offset: {block.Offset})");

			if (piece.IsPieceComplete())
			{
				_pieceManager.MarkPieceComplete(block.PieceIndex);
				BroadcastHaveMessage(block.PieceIndex, cToken);
				_logger.Info($"Broadcasted HAVE for Piece {block.PieceIndex}");

				if (_pieceManager.HasAllPieces() && _torrentSession.Pieces.All.All(p => p.IsPieceComplete()))
				{
					_torrentSession.OnFileCompleted.NotifyListeners(true);
					return;
				}

				if (!peer.IsChoked && peer.IsInterested)
				{
					_peerManager.QueuePieceRequests(peer, cToken);
					_logger.Info($"(Peer: {peer.PeerID ?? peer.IPEndPoint.ToString()}) Piece {block.PieceIndex} completed. Queuing new requests.");
				}
			}
		}

		private bool IsExpectedBlock(Peer peer, Block req)
		{
			return _torrentSession.PendingRequests.TryGetValue(peer, out var pending)
				   && pending.Keys.Any(b => Block.AreBlocksEqual(b, req));
		}
		
		private bool IsValidBlock(Block block)
		{
			if (block.PieceIndex < 0 || block.PieceIndex >= _torrentSession.Info.PieceHashes.Length)
				return false;

			if (block.Offset < 0 || block.Offset + block.Length > _peerManager.GetPieceLength(block.PieceIndex))
				return false;

			return true;
		}

		public void BroadcastHaveMessage(int pieceIndex, CancellationToken token)
		{
			var haveMessage = PeerMessage.CreateHaveMessage(pieceIndex);

			foreach (var peer in _torrentSession.ConnectedPeers)
			{
				if (token.IsCancellationRequested) break;

				// Optional: skip peers that already have the piece
				if (peer.AvailablePieces.Contains(pieceIndex)) continue;

				_peerManager.TryQueueMessage(peer, haveMessage);
			}
		}

		void HandleCancel(Peer peer, PeerMessage message)
		{
			var cancelBlock = Block.ParseCancelMessage(message.ToBytes());
			if (cancelBlock == null) return;

			_torrentSession.OutboundMessageQueues[peer].CancelBlock(cancelBlock);
			_logger.Info($"(Peer: {peer.PeerID}) Cancelled block (Piece: {cancelBlock.PieceIndex}, Offset: {cancelBlock.Offset})");
		}
	}
}
