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
		public ConcurrentSet<Peer> AllPeers { get; set; } = new();
		public ConcurrentSet<Peer> ConnectedPeers { get; set; } = new();
		public ConcurrentSet<Peer> AwaitingHandshakePeers { get; set; } = new();
		public ConcurrentDictionary<Peer, PeerTaskBundle> PeerTasks { get; set; } = new();
		public ConcurrentDictionary<Peer, OutboundMessageQueue> OutboundMessageQueues { get; set; } = new();
		public ConcurrentSet<Piece> Pieces { get; set; } = new();

		public ConcurrentDictionary<int, int> PieceRequestCounts = new();
		public ConcurrentDictionary<Peer, int> PeerRequestCounts = new();
		public ConcurrentDictionary<Peer, ConcurrentDictionary<Block, DateTime>> PendingRequests = new();

		public int TrackerIntervalSeconds { get; set; }

		public SessionEvent<ConcurrentSet<Peer>> OnTrackerResponse = new();
		public SessionEvent<(Peer, PeerMessage)> OnPeerMessageReceived = new();
		public SessionEvent<(Peer, Block)> OnBlockRequestReceived = new();
		public SessionEvent<(Peer, Block)> OnBlockReadFromDisk = new();
		public SessionEvent<Block> OnBlockReceived = new();
		public SessionEvent<Peer> OnPeerConnected = new();
		public SessionEvent<Peer> OnPeerDisconnected = new();
		public SessionEvent<bool> OnPiecesFlushed = new();
		public SessionEvent<PieceManagerSnapshot> OnPieceManagerSnapshotted = new();
		public SessionEvent<bool> OnFileCompleted = new();
		public SessionEvent<double> OnPauseCompleted = new();

		public bool ShouldFlushOnShutdown = false;
		public bool ShouldSnapshotOnShutdown = false;
		public PieceManagerSnapshot? PieceManagerSnapshot { get; set; }

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
				Info = info,
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

		public static TorrentSession CreateSessionFromSnapshot(PausedTorrentSessionSnapshot snapshot)
		{
			var peerId = "-WTOR01-" + Guid.NewGuid().ToString("N").Substring(0, 12);
			var info = snapshot.Info;
			var pieceLength = info.PieceLength.ConvertUnit(ByteUnit.Byte).Size;
			List<TorrentFile> files = info.IsMultiFile ? info.Files! : [new TorrentFile(info.Length!, [info.Name])];

			return new TorrentSession
			{
				Info = info,
				InfoHash = snapshot.InfoHash,
				LocalPeer = new Peer { PeerID = peerId, IPEndPoint = SessionConfig.LocalIpEndpoint },
				FileMap = new FileMap(pieceLength, files),
				TotalBytes = snapshot.TotalBytes,
				RemainingBytes = snapshot.RemainingBytes,
				CurrentEvent = EventState.Started,
				TrackerUrls = snapshot.TrackerUrls,
				CurrentTrackerUrlIndex = snapshot.CurrentTrackerUrlIndex,
				TrackerIntervalSeconds = 0,
				PieceManagerSnapshot = snapshot.PieceManagerSnapshot
			};
		}
	}
}
