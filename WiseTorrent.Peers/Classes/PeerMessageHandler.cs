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
					peer.IsInterested = true;
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
					_torrentSession.Metrics.RecordReceive(message.Payload.Length);
					peer.Metrics.RecordReceive(message.Payload.Length);
					break;

				case PeerMessageType.KeepAlive:
					peer.LastActive = DateTime.UtcNow;
					_logger.Info($"(Peer: {peer.PeerID ?? peer.IPEndPoint.ToString()}) Received keep-alive from peer");
					break;
				default:
					if (message.HandshakeMessage != null)
					{
						_logger.Info($"(Peer: {peer.PeerID ?? peer.IPEndPoint.ToString()}) Received handshake from peer");
						if (TryHandleHandshake(peer, message))
							SendBitfield(peer, message);
					}
					else
					{
						_logger.Warn($"Unknown message type from {peer.PeerID ?? peer.IPEndPoint.ToString()}: {message.MessageType}");
					}

					break;
			}

			peer.FollowsMessageOrder = IsMessageInOrder(peer, message);
			peer.LastReceived = DateTime.UtcNow;
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
		}

		private bool TryHandleHandshake(Peer peer, PeerMessage message)
		{
			if (message.HandshakeMessage == null) return false;

			var handshake = HandshakeMessage.FromBytes(message.HandshakeMessage.InfoHash);
			if (handshake == null || !handshake.InfoHash.SequenceEqual(_torrentSession.InfoHash))
				return false;

			peer.PeerID = handshake.PeerId;
			peer.IsConnected = true;
			peer.HandshakeCompleted = true;
			peer.ProtocolStage = PeerProtocolStage.AwaitingBitfield;
			_torrentSession.ConnectedPeers.Add(peer);

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

		private void HandleRequest(Peer peer, PeerMessage message, CancellationToken cToken)
		{
			var parsedBlock = Block.ParseRequestMessage(message.ToBytes());
			if (parsedBlock == null || !IsValidRequest(parsedBlock)) return;

			_torrentSession.OnBlockRequestReceived.NotifyListeners((peer, parsedBlock));
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
			if (parsedBlock != null && IsExpectedBlock(peer, parsedBlock) && IsValidBlock(parsedBlock))
			{
				if (peer.ProtocolStage == PeerProtocolStage.AwaitingPiece)
					peer.ProtocolStage = PeerProtocolStage.Established;
				var piece = _torrentSession.Pieces.FirstOrDefault(p => p.Index == parsedBlock.PieceIndex);
				if (piece != null && piece.Blocks.First(b => b.Offset == parsedBlock.Offset).Data == null)
				{
					if (_torrentSession.PendingRequests.TryGetValue(peer, out var pending))
					{
						var updated = new ConcurrentBag<Block>();
						foreach (var b in pending)
						{
							if (!b.Equals(parsedBlock))
								updated.Add(b);
						}
						_torrentSession.PendingRequests[peer] = updated;
					}

					piece.Validate();
					if (piece.State) _torrentSession.OnBlockReceived.NotifyListeners(parsedBlock);

					_pieceManager.MarkPieceComplete(parsedBlock.PieceIndex);
					_logger.Info($"(Peer: {peer.PeerID ?? peer.IPEndPoint.ToString()}) Received piece from peer, about (Piece Index, Block Offset): ({parsedBlock.PieceIndex}, {parsedBlock.Offset})");

					if (piece.IsPieceComplete())
					{
						BroadcastHaveMessage(parsedBlock.PieceIndex, cToken);
						_logger.Info($"Broad-casted have messages to all connected peers that don't have the piece, about Piece: {parsedBlock.PieceIndex}");
					}
				}
			}
		}

		private bool IsExpectedBlock(Peer peer, Block req)
		{
			return _torrentSession.PendingRequests[peer].Contains(req);
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

	}
}
