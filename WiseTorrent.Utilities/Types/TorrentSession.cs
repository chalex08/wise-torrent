using System.Text;

namespace WiseTorrent.Utilities.Types
{
    public class TorrentSession
    {
	    public required TorrentInfo Info { get; init; }
		public required byte[] InfoHash { get; init; }
		public required Peer LocalPeer { get; init; }

		public long UploadedBytes { get; set; }
		public long DownloadedBytes { get; set; }
		public long TotalBytes { get; set; }
		public long RemainingBytes { get; set; }
		public long ConnectionId { get; set; }
		public int LeecherCount { get; set; }
		public int SeederCount { get; set; }

		public EventState CurrentEvent { get; set; }

		public List<ServerURL> TrackerUrls { get; init; } = new();
		public int CurrentTrackerUrlIndex { get; set; }
		public ServerURL CurrentTrackerUrl => TrackerUrls[CurrentTrackerUrlIndex];
		public List<Peer> ConnectedPeers { get; set; } = new();

		public DateTime LastAnnounceTime { get; set; }
		public int TrackerIntervalSeconds { get; set; }

		public SessionEvent<List<Peer>> OnTrackerResponse = new();
	}
}
