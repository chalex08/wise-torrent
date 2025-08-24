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
			while (!pCToken.IsCancellationRequested)
			{
				byte[] receivedBytes = await PeerManager!.ReceivePeerMessageAsync(peer, pCToken);
				if (receivedBytes.Length > 0)
				{
					var message = PeerMessage.FromBytes(receivedBytes);
					if (message != null)
					{
						peer.LastReceived = DateTime.UtcNow;
						TorrentSession?.OnPeerMessageReceived.NotifyListeners((peer, message));
					}
				}
				else
				{
					_logger.Warn($"Peer {peer.PeerID} disconnected (received 0 bytes)");
					await PeerManager!.DisconnectPeerAsync(peer, pCToken);
					break;
				}
			}
		}
	}
}
