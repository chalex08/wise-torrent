using WiseTorrent.Peers.Interfaces;
using WiseTorrent.Utilities.Interfaces;
using WiseTorrent.Utilities.Types;

namespace WiseTorrent.Peers.Classes.ServiceTaskClients
{
	internal class KeepAliveServiceTaskClient : IPeerChildServiceTaskClient
	{
		private readonly ILogger<KeepAliveServiceTaskClient> _logger;
		public TorrentSession? TorrentSession { get; set; }
		public IPeerManager? PeerManager { get; set; }

		public KeepAliveServiceTaskClient(ILogger<KeepAliveServiceTaskClient> logger)
		{
			_logger = logger;
		}

		public async Task StartServiceTask(Peer peer, CancellationToken pCToken)
		{
			while (!pCToken.IsCancellationRequested)
			{
				try
				{
					var interval = TimeSpan.FromSeconds(SessionConfig.PeerKeepAliveIntervalSeconds);
					var idleTime = DateTime.UtcNow - peer.LastActive;

					if (idleTime > interval)
					{
						await PeerManager!.SendPeerMessageAsync(peer, PeerMessage.CreateKeepAlive().Payload, pCToken);
						_logger.Info($"Keep alive sent to {peer.PeerID} after {idleTime.TotalSeconds:F1}s idle");
					}

					await Task.Delay(interval, pCToken);
				}
				catch (Exception ex)
				{
					_logger.Error("Peer keep alive service loop encountered error", ex);
				}
			}
		}
	}
}
