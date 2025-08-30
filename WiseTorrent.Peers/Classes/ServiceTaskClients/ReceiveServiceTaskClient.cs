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

			while (!pCToken.IsCancellationRequested)
			{
				byte[] receivedBytes = await PeerManager!.ReceivePeerMessageAsync(peer, pCToken);
				var message = PeerMessage.FromBytes(receivedBytes);
				if (message != null)
				{
					peer.LastReceived = DateTime.UtcNow;
					TorrentSession?.OnPeerMessageReceived.NotifyListeners((peer, message));
				}
			}
		}
	}
}
