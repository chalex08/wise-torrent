using System.Net.Sockets;
using System.Net;

namespace WiseTorrent.Utilities.Types
{
	public static class SessionConfig
	{
		public static string TorrentStoragePath => "";
		public static IPEndPoint LocalIpEndpoint => new(Dns.GetHostEntry(Dns.GetHostName()).AddressList.First(a => a.AddressFamily == AddressFamily.InterNetwork), 6881);
		public static int PeerTimeoutSeconds => 300;
		public static int PeerKeepAliveIntervalSeconds => 300;
		public static TimeSpan PeerStateRefreshTimerSeconds => TimeSpan.FromSeconds(30);
		public static TimeSpan PeerReconnectCooldownSeconds => TimeSpan.FromSeconds(30);
		public static int MaxSwarmSize => 50;
		public static int MaxPeerConnectionThreads => (int)(0.2 * MaxSwarmSize);
		public static int MaxPeerReceiveThreads => (int)(1.0 * MaxSwarmSize);
		public static int MaxPeerSendThreads => (int)(1.5 * MaxSwarmSize);
		public static int MaxPeerKeepAliveThreads => (int)(0.4 * MaxSwarmSize);
		public static int MaxPeerUpdateStateThreads => (int)(0.6 * MaxSwarmSize);
		public static int MaxOutboundMessageQueueSize => 1_000;
		public static int BlockSizeBytes => 16_384;
		public static int MaxRequestsPerPeer => 32;
		public static int MaxRequestsPerPiece => 5;
		public static int MaxActivePieces => 16;
	}
}
