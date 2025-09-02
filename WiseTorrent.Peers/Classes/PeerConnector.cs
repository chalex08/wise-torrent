using System.Net.Sockets;
using WiseTorrent.Utilities.Interfaces;
using WiseTorrent.Utilities.Types;

namespace WiseTorrent.Peers.Classes
{
	public class PeerConnector : IDisposable
	{
		public PeerConnector(Peer peer, TorrentSession torrentSession, ILogger<PeerConnector> logger)
		{
			Peer = peer;
			TorrentSession = torrentSession;
			Client = new TcpClient();
			_logger = logger;
		}

		private readonly ILogger<PeerConnector> _logger;
		private bool _disposed;
		private Peer Peer { get; }
		private TorrentSession TorrentSession { get; }
		private TcpClient Client { get; }
		private NetworkStream? _stream;

		public async Task InitiateHandshakeAsync(CancellationToken token)
		{
			try
			{
				_logger.Info($"Attempting connection (PeerID: {Peer.PeerID ?? Peer.IPEndPoint.ToString()})");
				await Client.ConnectAsync(Peer.IPEndPoint.Address, Peer.IPEndPoint.Port, token);
				_stream = Client.GetStream();

				_logger.Info($"Connection successful. Attempting handshake (PeerID: {Peer.PeerID ?? Peer.IPEndPoint.ToString()})");
				var handshake = new PeerMessage(new HandshakeMessage(TorrentSession.InfoHash, Peer.PeerID!));
				TorrentSession.OutboundMessageQueues[Peer].TryEnqueue(handshake);
				Peer.ProtocolStage = PeerProtocolStage.AwaitingHandshake;
			}
			catch (Exception ex)
			{
				_logger.Error($"Failed to connect to {Peer.IPEndPoint}: {ex.Message}");
			}
		}

		public async Task<byte[]> ReceiveHandshakeAsync(CancellationToken token)
		{
			var buffer = new byte[68];
			int offset = 0;
			while (offset < 68)
			{
				if (Client.Client.Available == 0)
				{
					await Task.Delay(10, token);
					continue;
				}

				int read = await TryReceiveIntoBufferAsync(buffer, offset, 68 - offset, token);
				if (read == 0)
				{
					_logger.Error("Peer seemingly disconnected during handshake");
				}
				offset += read;
			}
			return buffer;
		}


		public async Task<int> TryReceiveIntoBufferAsync(byte[] buffer, int offset, int count, CancellationToken token)
		{
			if (_stream == null || !_stream.CanRead) return 0;
			try
			{
				int bytesRead = await _stream.ReadAsync(buffer, offset, count, token);
				if (bytesRead > 0)
				{
					TorrentSession.Metrics.RecordReceive(bytesRead);
					Peer.Metrics.RecordReceive(bytesRead);
					Peer.LastActive = DateTime.UtcNow;
				}
				return bytesRead;
			}
			catch (Exception ex)
			{
				_logger.Warn($"Receive error from {Peer.IPEndPoint}: {ex.Message}");
				return 0;
			}
		}

		public async Task<bool> TrySendPeerMessageAsync(byte[] data, CancellationToken cToken)
		{
			if (_stream == null || !_stream.CanWrite) return false;

			try
			{
				await _stream.WriteAsync(data, 0, data.Length, cToken);
				Peer.LastActive = DateTime.UtcNow;
				return true;
			}
			catch (Exception ex)
			{
				_logger.Error($"Failed to send message to peer (PeerID: {Peer.PeerID ?? Peer.IPEndPoint.ToString()})", ex);
				return false;
			}
		}

		public async Task<byte[]> TryReceivePeerMessageAsync(CancellationToken token)
		{
			if (_stream == null || !_stream.CanRead) return [];

			// read the 4-byte length prefix
			byte[] lengthBuffer = new byte[4];
			int read = 0;
			while (read < 4)
			{
				if (Client.Client.Available == 0)
				{
					await Task.Delay(10, token);
					continue;
				}
				int chunk = await _stream.ReadAsync(lengthBuffer, read, 4 - read, token);
				if (chunk == 0) _logger.Error("Peer seemingly disconnected during length prefix read");
				read += chunk;
			}

			// convert to int (big-endian)
			int length = (lengthBuffer[0] << 24) |
						 (lengthBuffer[1] << 16) |
						 (lengthBuffer[2] << 8) |
						 lengthBuffer[3];

			// handle keep-alive (length = 0)
			if (length == 0)
				return lengthBuffer; // Just return the prefix for keep-alive

			// read the rest of the message
			byte[] messageBuffer = new byte[length];
			int offset = 0;
			while (offset < length)
			{
				if (Client.Client.Available == 0)
				{
					await Task.Delay(10, token);
					continue;
				}
				int chunk = await _stream.ReadAsync(messageBuffer, offset, length - offset, token);
				if (chunk == 0) _logger.Error("Peer seemingly disconnected during message read");
				offset += chunk;
			}

			// combine prefix + message
			byte[] fullMessage = new byte[4 + length];
			Buffer.BlockCopy(lengthBuffer, 0, fullMessage, 0, 4);
			Buffer.BlockCopy(messageBuffer, 0, fullMessage, 4, length);

			TorrentSession.Metrics.RecordReceive(fullMessage.Length);
			Peer.Metrics.RecordReceive(fullMessage.Length);
			Peer.LastActive = DateTime.UtcNow;

			return fullMessage;
		}

		public async Task DisconnectAsync(CancellationToken cToken)
		{
			if (_disposed) return;

			try
			{
				_stream?.Flush();
				_stream?.Close();
				Client.Close();

				Peer.IsConnected = false;
				Peer.LastActive = DateTime.UtcNow;
				TorrentSession.PeerTasks.TryRemove(Peer, out var peerTasks);
				if (peerTasks == null) return;

				await peerTasks.CTS.CancelAsync();
				await Task.WhenAll(peerTasks.Tasks);
				peerTasks.CTS.Dispose();

				_logger.Info($"Disconnected from peer {Peer.PeerID ?? Peer.IPEndPoint.ToString()}, and stopped all child service tasks");
			}
			catch (Exception ex)
			{
				_logger.Warn($"Error during disconnect: {ex.Message}");
			}
			finally
			{
				Dispose();
			}
		}

		private void Disconnect(bool disposing)
		{
			if (_disposed) return;
			if (disposing)
			{
				_stream?.Dispose();
				Client.Dispose();
			}
			_disposed = true;
		}

		public void Dispose()
		{
			Disconnect(true);
			GC.SuppressFinalize(this);
		}
	}
}
