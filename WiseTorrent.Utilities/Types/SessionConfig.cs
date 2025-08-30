using System.Net.Sockets;
using System.Net;

namespace WiseTorrent.Utilities.Types
{
	public static class SessionConfig
	{
		public static string TorrentStoragePath { get; set; } = "";
		public static IPEndPoint LocalIpEndpoint { get; set; } = new(Dns.GetHostEntry(Dns.GetHostName()).AddressList.First(a => a.AddressFamily == AddressFamily.InterNetwork), 6881);
		public static int PeerTimeoutSeconds { get; set; } = 300;
		public static int PeerKeepAliveIntervalSeconds { get; set; } = 300;
		public static TimeSpan PeerStateRefreshTimerSeconds { get; set; } = TimeSpan.FromSeconds(30);
		public static TimeSpan PeerReconnectCooldownSeconds { get; set; } = TimeSpan.FromSeconds(30);
		public static int MaxSwarmSize { get; set; } = 50;
		public static int MaxPeerConnectionThreads { get; set; } = (int)(0.2 * MaxSwarmSize);
		public static int MaxPeerReceiveThreads { get; set; } = (int)(1.0 * MaxSwarmSize);
		public static int MaxPeerSendThreads { get; set; } = (int)(1.5 * MaxSwarmSize);
		public static int MaxPeerKeepAliveThreads { get; set; } = (int)(0.4 * MaxSwarmSize);
		public static int MaxPeerUpdateStateThreads { get; set; } = (int)(0.6 * MaxSwarmSize);
		public static int MaxOutboundMessageQueueSize { get; set; } = 1_000;
		public static int BlockSizeBytes { get; set; } = 16_384;
		public static int MaxRequestsPerPeer { get; set; } = 32;
		public static int MaxRequestsPerPiece { get; set; } = 5;
		public static int MaxActivePieces { get; set; } = 16;
	}
}
