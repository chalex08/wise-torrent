using WiseTorrent.Peers.Interfaces;
using WiseTorrent.Utilities.Interfaces;
using WiseTorrent.Utilities.Types;

namespace WiseTorrent.Peers.Classes.ServiceTaskClients
{
	internal class SendServiceTaskClient : IPeerChildServiceTaskClient
	{
		private readonly ILogger<SendServiceTaskClient> _logger;
		public TorrentSession? TorrentSession { get; set; }
		public IPeerManager? PeerManager { get; set; }

		public SendServiceTaskClient(ILogger<SendServiceTaskClient> logger)
		{
			_logger = logger;
		}

		public async Task StartServiceTask(Peer peer, CancellationToken pCToken)
		{
			if (TorrentSession == null || PeerManager == null)
				throw new InvalidOperationException("Dependencies not set");

			try
			{
				while (!pCToken.IsCancellationRequested)
				{
					var message = await TorrentSession.OutboundMessageQueues[peer].DequeueAsync(pCToken);
					if (message == null) continue;

					var bytes = message.ToBytes();
					await PeerManager.SendPeerMessageAsync(peer, bytes, pCToken);

					peer.LastActive = DateTime.UtcNow;
				}
			}
			catch (OperationCanceledException)
			{
				_logger.Info($"Send loop cancelled for peer {peer.PeerID}");
			}
			catch (Exception ex)
			{
				_logger.Warn($"Send loop error for peer {peer.PeerID}: {ex.Message}");
			}

		}
	}
}
