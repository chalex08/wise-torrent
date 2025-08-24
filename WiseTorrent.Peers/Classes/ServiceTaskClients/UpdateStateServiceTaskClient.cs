using WiseTorrent.Peers.Interfaces;
using WiseTorrent.Utilities.Interfaces;
using WiseTorrent.Utilities.Types;

namespace WiseTorrent.Peers.Classes.ServiceTaskClients
{
	internal class UpdateStateServiceTaskClient : IPeerSiblingServiceTaskClient
	{
		private readonly ILogger<UpdateStateServiceTaskClient> _logger;
		public TorrentSession? TorrentSession { get; set; }
		public IPeerManager? PeerManager { get; set; }

		public UpdateStateServiceTaskClient(ILogger<UpdateStateServiceTaskClient> logger)
		{
			_logger = logger;
		}

		public async Task StartServiceTask(CancellationToken pCToken)
		{
			if (TorrentSession == null || PeerManager == null)
				throw new InvalidOperationException("Dependencies not set");

			while (!pCToken.IsCancellationRequested)
			{
				try
				{
					await Task.Delay(SessionConfig.PeerStateRefreshTimerSeconds, pCToken);
					PeerManager!.UpdatePeerStatesAsync(pCToken);
					await PeerManager!.UpdatePeerSelectionAsync(pCToken);
				}
				catch (Exception ex)
				{
					_logger.Error("Update peer state service loop encountered error", ex);
				}
			}
		}
	}
}
