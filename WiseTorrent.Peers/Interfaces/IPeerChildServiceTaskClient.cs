using WiseTorrent.Utilities.Types;

namespace WiseTorrent.Peers.Interfaces
{
	interface IPeerChildServiceTaskClient
	{
		TorrentSession? TorrentSession { get; set; }
		IPeerManager? PeerManager { get; set; }

		Task StartServiceTask(Peer peer, CancellationToken pCToken);
	}
}
