using System.Collections.Concurrent;

namespace WiseTorrent.Utilities.Types
{
    public class TorrentSession
    {
	    public CancellationTokenSource Cts { get; } = new();

	    public required TorrentInfo Info { get; init; }
		public required byte[] InfoHash { get; init; }
		public required Peer LocalPeer { get; init; }
		public required FileMap FileMap { get; init; }

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
		public List<Piece> Pieces { get; set; } = new();

		public ConcurrentDictionary<int, int> PieceRequestCounts = new();
		public ConcurrentDictionary<Peer, int> PeerRequestCounts = new();
		public ConcurrentDictionary<Peer, ConcurrentBag<Block>> PendingRequests = new();

		public int TrackerIntervalSeconds { get; set; }

		public SessionEvent<List<Peer>> OnTrackerResponse = new();
		public SessionEvent<(Peer, PeerMessage)> OnPeerMessageReceived = new();
		public SessionEvent<(Peer, Block)> OnBlockRequestReceived = new();
		public SessionEvent<(Peer, Block)> OnBlockReadFromDisk = new();
		public SessionEvent<Block> OnBlockReceived = new();
		public SessionEvent<Peer> OnPeerConnected = new();
		public SessionEvent<Peer> OnPeerDisconnected = new();

		public static TorrentSession CreateSessionFromMetadata(TorrentMetadata torrentMetadata)
		{
			var peerId = "-WTOR01-" + Guid.NewGuid().ToString("N").Substring(0, 12);
			var info = torrentMetadata.Info;
			var totalBytes = info.IsMultiFile
				? info.Files!.Select(f => f.Length.ConvertUnit(ByteUnit.Byte).Size).Sum()
				: info.Length!.ConvertUnit(ByteUnit.Byte).Size;
			var pieceLength = info.PieceLength.ConvertUnit(ByteUnit.Byte).Size;
			List<TorrentFile> files = info.IsMultiFile ? info.Files! : [new TorrentFile(info.Length!, [info.Name])];

			return new TorrentSession
			{
				Info = torrentMetadata.Info,
				InfoHash = torrentMetadata.InfoHash,
				LocalPeer = new Peer { PeerID = peerId, IPEndPoint = SessionConfig.LocalIpEndpoint },
				FileMap = new FileMap(pieceLength, files),
				TotalBytes = totalBytes,
				RemainingBytes = totalBytes,
				CurrentEvent = EventState.Started,
				TrackerUrls = torrentMetadata.AnnounceList?.SelectMany(urls => urls).ToList() ?? [torrentMetadata.Announce!],
				CurrentTrackerUrlIndex = 0,
				TrackerIntervalSeconds = 0
			};
		}
	}
}
