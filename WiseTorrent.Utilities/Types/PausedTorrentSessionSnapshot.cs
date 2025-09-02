using System.Text.Json.Serialization;

namespace WiseTorrent.Utilities.Types
{
	public class PausedTorrentSessionSnapshot
	{
		public required TorrentInfo Info { get; init; }
		public required byte[] InfoHash { get; init; }
		public required FileMap FileMap { get; init; }
		public long TotalBytes { get; init; }
		public long RemainingBytes { get; init; }
		public List<ServerURL> TrackerUrls { get; init; } = new();
		public int CurrentTrackerUrlIndex { get; init; }

		[JsonIgnore] 
		public ConcurrentSet<Piece> Pieces { get; init; } = new();
		[JsonPropertyName("Pieces")]
		public List<Piece> PiecesSnapshot
		{
			get => Pieces.All.ToList();
			init => Pieces = new ConcurrentSet<Piece>(value);
		}

		public PieceManagerSnapshot PieceManagerSnapshot { get; init; } = new();

		public static PausedTorrentSessionSnapshot CreateSnapshotOfSession(TorrentSession session, PieceManagerSnapshot pieceManagerSnapshot)
		{
			return new PausedTorrentSessionSnapshot
			{
				Info = session.Info,
				InfoHash = session.InfoHash,
				FileMap = session.FileMap,
				TotalBytes = session.TotalBytes,
				RemainingBytes = session.RemainingBytes,
				TrackerUrls = session.TrackerUrls,
				CurrentTrackerUrlIndex = session.CurrentTrackerUrlIndex,
				Pieces = session.Pieces,
				PieceManagerSnapshot = pieceManagerSnapshot
			};
		}
	}
}
