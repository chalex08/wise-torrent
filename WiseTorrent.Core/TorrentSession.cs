using WiseTorrent.Parsing.Types;
using WiseTorrent.Peers.Types;
using WiseTorrent.Trackers.Interfaces;
using WiseTorrent.Trackers.Types;

namespace WiseTorrent.Core
{
    public class TorrentSession
    {
	    public required TorrentInfo Info { get; init; }
		public required byte[] InfoHash { get; init; }
		public required string TorrentName { get; init; }
		public required Peer LocalPeer { get; init; }

		public long UploadedBytes { get; set; }
		public long DownloadedBytes { get; set; }
		public long RemainingBytes { get; set; }

		public EventState CurrentEvent { get; set; }

		public required List<ServerURL> TrackerUrls { get; init; }
		public List<Peer> ConnectedPeers { get; set; } = new();

		public DateTime LastAnnounceTime { get; set; }
		public int TrackerIntervalSeconds { get; set; }
	}
}
