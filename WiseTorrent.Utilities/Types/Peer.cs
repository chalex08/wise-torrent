using System.Collections.Concurrent;
using System.Net;
using System.Text;

namespace WiseTorrent.Utilities.Types
{
	public class Peer
	{
		// Identification
		public string? PeerID { get; set; }
		public required IPEndPoint IPEndPoint { get; set; }
		private byte[]? _peerIdBytes;
		public byte[] PeerIDBytes => _peerIdBytes ??= Encoding.UTF8.GetBytes(PeerID ?? "");

		// Connection State
		public bool IsConnected { get; set; } = false;
		public DateTime LastActive { get; set; }
		public DateTime LastReceived { get; set; }

		// Protocol State
		public bool HandshakeCompleted { get; set; } = false;
		public bool BitfieldReceived { get; set; } = false;
		public bool FollowsMessageOrder { get; set; } = true;
		public PeerProtocolStage ProtocolStage;
		public bool IsChoked { get; set; } = true;
		public bool IsInterested { get; set; } = false;
		public HashSet<int> AvailablePieces { get; set; } = new();
		public ConcurrentDictionary<Block, DateTime> PendingPieceRequests = new();

		// Transfer Metrics 
		public PeerMetricsCollector Metrics { get; init; } = new();
		public PeerMetricsSnapshot Snapshot { get; set; } = new();
		public int TimeoutCount { get; set; }
		public int RarePiecesHeldCount { get; set; }
		public bool HasAllPieces { get; set; }
		public double DecayMultiplier { get; set; } = 1;

		// Performance scoring
		public int CalculatePeerScore()
		{
			Snapshot = Metrics.GetSnapshot();
			var responsivenessScore = GetResponsivenessScore(); // 0–25
			var reliabilityScore = GetReliabilityScore(); // 0–20
			var protocolScore = GetProtocolComplianceScore(); // 0–15
			var throughputScore = GetThroughputScore(); // 0–25
			var swarmValueScore = GetSwarmValueScore(); // 0–15

			var totalScore = responsivenessScore + reliabilityScore + protocolScore + throughputScore + swarmValueScore;
			if (TimeoutCount >= 1 && totalScore > 80)
				totalScore = 80;
			else if (TimeoutCount >= 3 && totalScore > 40)
				totalScore = 40;

			return (int)(totalScore * DecayMultiplier);
		}

		private int GetResponsivenessScore()
		{
			TimeSpan avgResponseTime = Snapshot.AverageResponseTime; // e.g. from handshake, bitfield, piece replies
			if (avgResponseTime < TimeSpan.FromMilliseconds(200)) return 25;
			if (avgResponseTime < TimeSpan.FromSeconds(1)) return 20;
			if (avgResponseTime < TimeSpan.FromSeconds(3)) return 10;
			return 0;
		}

		private int GetReliabilityScore()
		{
			int timeoutCount = TimeoutCount;
			if (timeoutCount == 0) return 20;
			if (timeoutCount == 1) return 15;
			if (timeoutCount == 2) return 10;
			return 0;
		}

		private int GetProtocolComplianceScore()
		{
			int score = 0;
			if (HandshakeCompleted) score += 5;
			if (BitfieldReceived) score += 5;
			if (FollowsMessageOrder) score += 5; // handshake -> bitfield -> have/request -> piece
			return score;
		}

		private int GetThroughputScore()
		{
			double downloadRate = Snapshot.DownloadRate;
			double uploadRate = Snapshot.UploadRate;

			int score = 0;
			if (downloadRate > 100_000) score += 15;
			else if (downloadRate > 10_000) score += 10;

			if (uploadRate > 50_000) score += 10;
			else if (uploadRate > 5_000) score += 5;

			return score;
		}

		private int GetSwarmValueScore()
		{
			int rarePiecesHeld = RarePiecesHeldCount;
			bool isSeeder = HasAllPieces;

			if (isSeeder) return 15;
			if (rarePiecesHeld > 5) return 10;
			if (rarePiecesHeld > 0) return 5;
			return 0;
		}

		public void ResetDecay()
		{
			DecayMultiplier = 1;
		}

		public void DecayScore()
		{
			DecayMultiplier *= 0.95;
		}

		public byte[] BuildBitfield(int totalPieces)
		{
			int byteCount = (totalPieces + 7) / 8;
			byte[] bitfield = new byte[byteCount];

			foreach (int pieceIndex in AvailablePieces)
			{
				if (pieceIndex < 0 || pieceIndex >= totalPieces)
					continue;

				int byteIndex = pieceIndex / 8;
				int bitIndex = 7 - (pieceIndex % 8); // most significant bit first

				bitfield[byteIndex] |= (byte)(1 << bitIndex);
			}

			return bitfield;
		}

		public override bool Equals(object? obj)
		{
			if (obj is not Peer other) return false;
			return IPEndPoint.Equals(other.IPEndPoint);
		}

		public override int GetHashCode() => IPEndPoint.GetHashCode();

		public override string ToString() => $"{IPEndPoint} - ID: {PeerID ?? "Unknown"} - Connected: {IsConnected} - Score: {CalculatePeerScore():F2}";
	}
}
