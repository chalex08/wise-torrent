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
			Client = new TcpClient(SessionConfig.LocalIpEndpoint);
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
				_logger.Info($"Attempting connection (PeerID: {Peer.PeerID})");
				await Client.ConnectAsync(Peer.IPEndPoint.Address, Peer.IPEndPoint.Port, token);
				_stream = Client.GetStream();

				_logger.Info($"Connection successful. Attempting handshake (PeerID: {Peer.PeerID})");
				var handshake = new PeerMessage(new HandshakeMessage(TorrentSession.InfoHash, TorrentSession.LocalPeer.PeerID!));
				TorrentSession.OutboundMessageQueues[Peer].TryEnqueue(handshake);
				Peer.ProtocolStage = PeerProtocolStage.AwaitingHandshake;
			}
			catch (Exception ex)
			{
				_logger.Error($"Failed to connect to {Peer.IPEndPoint}: {ex.Message}");
			}
		}

		public async Task<bool> TrySendPeerMessageAsync(byte[] data, CancellationToken cToken)
		{
			if (_stream == null || !_stream.CanWrite) return false;

			try
			{
				await _stream.WriteAsync(data, 0, data.Length, cToken);
				TorrentSession.Metrics.RecordSend(data.Length);
				Peer.Metrics.RecordSend(data.Length);
				Peer.LastActive = DateTime.UtcNow;
				return true;
			}
			catch
			{
				return false;
			}
		}

		public async Task<byte[]> TryReceivePeerMessageAsync(CancellationToken cToken)
		{
			if (_stream == null || !_stream.CanRead) return [];

			byte[] buffer = new byte[4096];
			try
			{
				int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, cToken);
				if (bytesRead > 0)
				{
					TorrentSession.Metrics.RecordReceive(bytesRead);
					Peer.Metrics.RecordReceive(bytesRead);
					Peer.LastActive = DateTime.UtcNow;
				}
				return bytesRead > 0 ? buffer[..bytesRead] : [];
			}
			catch
			{
				return [];
			}
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
				var peerTasks = TorrentSession.PeerTasks[Peer];
				await peerTasks.CTS.CancelAsync();
				await Task.WhenAll(peerTasks.Tasks);
				TorrentSession.PeerTasks.Remove(Peer);
				peerTasks.CTS.Dispose();

				_logger.Info($"Disconnected from peer {Peer.PeerID}, and stopped all child service tasks");
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
