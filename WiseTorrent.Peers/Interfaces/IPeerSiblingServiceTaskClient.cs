using WiseTorrent.Utilities.Types;

namespace WiseTorrent.Peers.Interfaces
{
	interface IPeerSiblingServiceTaskClient
	{
		TorrentSession? TorrentSession { get; set; }
		IPeerManager? PeerManager { get; set; }

		Task StartServiceTask(CancellationToken pCToken);
	}
}
