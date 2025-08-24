namespace WiseTorrent.Utilities.Types
{
    public class TorrentSession
    {
	    public required TorrentInfo Info { get; init; }
		public required byte[] InfoHash { get; init; }
		public required Peer LocalPeer { get; init; }

		public TorrentSessionMetricsCollector Metrics { get; init; } = new();
		public long TotalBytes { get; set; }
		public long RemainingBytes { get; set; }
		public long ConnectionId { get; set; }
		public int LeecherCount { get; set; }
		public int SeederCount { get; set; }

		public EventState CurrentEvent { get; set; }

		public List<ServerURL> TrackerUrls { get; init; } = new();
		public int CurrentTrackerUrlIndex { get; set; }
		public ServerURL CurrentTrackerUrl => TrackerUrls[CurrentTrackerUrlIndex];
		public List<Peer> AllPeers { get; set; } = new();
		public List<Peer> ConnectedPeers { get; set; } = new();
		public Dictionary<Peer, PeerTaskBundle> PeerTasks { get; set; } = new();
		public Dictionary<Peer, OutboundMessageQueue> OutboundMessageQueues { get; set; } = new();

		public int TrackerIntervalSeconds { get; set; }

		public SessionEvent<List<Peer>> OnTrackerResponse = new();
		public SessionEvent<(Peer, PeerMessage)> OnPeerMessageReceived = new();
		public SessionEvent<Block> OnBlockReceived = new();
		public SessionEvent<Peer> OnPeerConnected = new();
		public SessionEvent<Peer> OnPeerDisconnected = new();
	}
}
