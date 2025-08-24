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

		public async Task ConnectAsync(CancellationToken token)
		{
			try
			{
				_logger.Info($"Attempting connection (PeerID: {Peer.PeerID})");
				await Client.ConnectAsync(Peer.IPEndPoint.Address, Peer.IPEndPoint.Port, token);
				var stream = Client.GetStream();

				_logger.Info($"Connection successful. Attempting handshake (PeerID: {Peer.PeerID})");
				var handshake = new Handshake();
				var handshakeBytes = handshake.CreateHandshake(TorrentSession.InfoHash, TorrentSession.LocalPeer.PeerID!);
				await stream.WriteAsync(handshakeBytes, 0, handshakeBytes.Length, token);

				byte[] response = new byte[68];
				int bytesRead = await stream.ReadAsync(response, 0, response.Length, token);

				if (bytesRead != 68 || !handshake.TryParseHandshake(response, out var receivedInfoHash, out var receivedPeerId))
					throw new InvalidOperationException("Invalid handshake response.");

				if (!handshake.IsValidHandshake(receivedInfoHash, TorrentSession.InfoHash))
					throw new InvalidOperationException("InfoHash mismatch.");

				Peer.PeerID = receivedPeerId;
				Peer.IsConnected = true;
				Peer.LastActive = DateTime.UtcNow;
				_stream = stream;
				_logger.Info($"Handshake successful. Peer now connected (PeerID: {Peer.PeerID})");
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
				TorrentSession.UploadedBytes += data.Length;
				Peer.UploadedBytes += data.Length;
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
					TorrentSession.DownloadedBytes += bytesRead;
					Peer.DownloadedBytes += bytesRead;
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
