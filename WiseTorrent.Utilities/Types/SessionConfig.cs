using System.Net.Sockets;
using System.Net;

namespace WiseTorrent.Utilities.Types
{
	public static class SessionConfig
	{
		public static IPEndPoint LocalIpEndpoint => new(Dns.GetHostEntry(Dns.GetHostName()).AddressList.First(a => a.AddressFamily == AddressFamily.InterNetwork), 6881);

		// Default values
		public static readonly string DefaultTorrentStoragePath = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
			"Downloads"
		);
		public static readonly int DefaultPeerTimeoutSeconds = 300;
		public static readonly int DefaultPeerKeepAliveIntervalSeconds = 300;
		public static readonly TimeSpan DefaultPeerStateRefreshTimerSeconds = TimeSpan.FromSeconds(30);
		public static readonly TimeSpan DefaultPeerReconnectCooldownSeconds = TimeSpan.FromSeconds(30);
		public static readonly int DefaultMaxSwarmSize = 50;
		public static readonly int DefaultMaxPeerConnectionThreads = (int)(0.2 * DefaultMaxSwarmSize);
		public static readonly int DefaultMaxPeerReceiveThreads = (int)(1.0 * DefaultMaxSwarmSize);
		public static readonly int DefaultMaxPeerSendThreads = (int)(1.5 * DefaultMaxSwarmSize);
		public static readonly int DefaultMaxPeerKeepAliveThreads = (int)(0.4 * DefaultMaxSwarmSize);
		public static readonly int DefaultMaxPeerUpdateStateThreads = (int)(0.6 * DefaultMaxSwarmSize);
		public static readonly int DefaultMaxOutboundMessageQueueSize = 1_000;
		public static readonly int DefaultBlockSizeBytes = 16_384;
		public static readonly int DefaultMaxRequestsPerPeer = 32;
		public static readonly int DefaultMaxRequestsPerPiece = 5;
		public static readonly int DefaultMaxActivePieces = 16;
		public static readonly int DefaultPieceRarityThreshold = 5;
		public static readonly TimeSpan DefaultPieceRequestTimeoutLimitSeconds = TimeSpan.FromSeconds(5);
		public static readonly int DefaultMaxLogEntriesCount = 1_000;
		public static readonly int DefaultLogRefreshThreshold = 10;
		public static readonly TimeSpan DefaultLogRefreshIntervalSeconds = TimeSpan.FromSeconds(1);

        public static string TorrentStoragePath { get; set; } = DefaultTorrentStoragePath;
        public static int PeerTimeoutSeconds { get; set; } = DefaultPeerTimeoutSeconds;
        public static int PeerKeepAliveIntervalSeconds { get; set; } = DefaultPeerKeepAliveIntervalSeconds;
        public static TimeSpan PeerStateRefreshTimerSeconds { get; set; } = DefaultPeerStateRefreshTimerSeconds;
        public static TimeSpan PeerReconnectCooldownSeconds { get; set; } = DefaultPeerReconnectCooldownSeconds;
        public static int MaxSwarmSize { get; set; } = DefaultMaxSwarmSize;
        public static int MaxPeerConnectionThreads { get; set; } = DefaultMaxPeerConnectionThreads;
        public static int MaxPeerReceiveThreads { get; set; } = DefaultMaxPeerReceiveThreads;
        public static int MaxPeerSendThreads { get; set; } = DefaultMaxPeerSendThreads;
        public static int MaxPeerKeepAliveThreads { get; set; } = DefaultMaxPeerKeepAliveThreads;
        public static int MaxPeerUpdateStateThreads { get; set; } = DefaultMaxPeerUpdateStateThreads;
        public static int MaxOutboundMessageQueueSize { get; set; } = DefaultMaxOutboundMessageQueueSize;
        public static int BlockSizeBytes { get; set; } = DefaultBlockSizeBytes;
        public static int MaxRequestsPerPeer { get; set; } = DefaultMaxRequestsPerPeer;
        public static int MaxRequestsPerPiece { get; set; } = DefaultMaxRequestsPerPiece;
        public static int MaxActivePieces { get; set; } = DefaultMaxActivePieces;
		public static int PieceRarityThreshold { get; set; } = DefaultPieceRarityThreshold;
		public static TimeSpan PieceRequestTimeoutLimitSeconds { get; set; } = DefaultPieceRequestTimeoutLimitSeconds;
		public static int MaxLogEntriesCount { get; set; } = DefaultMaxLogEntriesCount;
		public static int LogRefreshThreshold { get; set; } = DefaultLogRefreshThreshold;
		public static TimeSpan LogRefreshIntervalSeconds { get; set; } = DefaultLogRefreshIntervalSeconds;

		public static void ResetDefaultValues()
        {
			TorrentStoragePath = DefaultTorrentStoragePath;
			PeerTimeoutSeconds = DefaultPeerTimeoutSeconds;
			PeerKeepAliveIntervalSeconds = DefaultPeerKeepAliveIntervalSeconds;
			PeerStateRefreshTimerSeconds = DefaultPeerStateRefreshTimerSeconds;
			PeerReconnectCooldownSeconds = DefaultPeerReconnectCooldownSeconds;
			MaxSwarmSize = DefaultMaxSwarmSize;
			MaxPeerConnectionThreads = DefaultMaxPeerConnectionThreads;
			MaxPeerReceiveThreads = DefaultMaxPeerReceiveThreads;
			MaxPeerSendThreads = DefaultMaxPeerSendThreads;
			MaxPeerKeepAliveThreads = DefaultMaxPeerKeepAliveThreads;
			MaxPeerUpdateStateThreads = DefaultMaxPeerUpdateStateThreads;
			MaxOutboundMessageQueueSize = DefaultMaxOutboundMessageQueueSize;
			BlockSizeBytes = DefaultBlockSizeBytes;
			MaxRequestsPerPeer = DefaultMaxRequestsPerPeer;
			MaxRequestsPerPiece = DefaultMaxRequestsPerPiece;
			MaxActivePieces = DefaultMaxActivePieces;
			PieceRarityThreshold = DefaultPieceRarityThreshold;
			PieceRequestTimeoutLimitSeconds = DefaultPieceRequestTimeoutLimitSeconds;
			MaxLogEntriesCount = DefaultMaxLogEntriesCount;
			LogRefreshThreshold = DefaultLogRefreshThreshold;
			LogRefreshIntervalSeconds = DefaultLogRefreshIntervalSeconds;
		}
    }
}
