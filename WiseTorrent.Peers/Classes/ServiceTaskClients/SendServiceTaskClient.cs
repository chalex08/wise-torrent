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
		}
	}
}
