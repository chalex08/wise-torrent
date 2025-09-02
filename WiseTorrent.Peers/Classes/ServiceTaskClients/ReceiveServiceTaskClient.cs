using WiseTorrent.Peers.Interfaces;
using WiseTorrent.Utilities.Interfaces;
using WiseTorrent.Utilities.Types;

namespace WiseTorrent.Peers.Classes.ServiceTaskClients
{
	internal class ReceiveServiceTaskClient : IPeerChildServiceTaskClient
	{
		private readonly ILogger<ReceiveServiceTaskClient> _logger;
		public TorrentSession? TorrentSession { get; set; }
		public IPeerManager? PeerManager { get; set; }

		public ReceiveServiceTaskClient(ILogger<ReceiveServiceTaskClient> logger)
		{
			_logger = logger;
		}

		public async Task StartServiceTask(Peer peer, CancellationToken pCToken)
		{
			if (TorrentSession == null || PeerManager == null)
				throw new InvalidOperationException("Dependencies not set");

			await ReceiveHandshakeMessage(peer, pCToken);

			while (!pCToken.IsCancellationRequested)
			{
				try
				{
					await Task.Delay(1, pCToken);
					byte[] receivedBytes = await PeerManager!.ReceivePeerMessageAsync(peer, pCToken);
					var message = PeerMessage.FromBytes(receivedBytes);
					if (message != null)
					{
						peer.LastReceived = DateTime.UtcNow;
						TorrentSession?.OnPeerMessageReceived.NotifyListeners((peer, message));
					}
				}
				catch (OperationCanceledException)
				{
					_logger.Info("Peer receive service loop stopped");
					break;
				}
			}
		}

		private async Task ReceiveHandshakeMessage(Peer peer, CancellationToken pCToken)
		{
			byte[] handshakeBytes = await PeerManager!.ReceiveHandshakeAsync(peer, pCToken);
			var handshake = HandshakeMessage.FromBytes(handshakeBytes);
			if (handshake == null)
			{
				_logger.Warn($"Invalid handshake from {peer.IPEndPoint}");
				await PeerManager!.DisconnectPeerAsync(peer, pCToken);
				return;
			}

			peer.PeerID = handshake.PeerId;
			peer.LastReceived = DateTime.UtcNow;
			TorrentSession?.OnPeerMessageReceived.NotifyListeners((peer, new PeerMessage(handshake)));
		}
	}
}
