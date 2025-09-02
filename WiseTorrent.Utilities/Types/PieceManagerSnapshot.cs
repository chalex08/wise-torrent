using System.Text.Json.Serialization;

namespace WiseTorrent.Utilities.Types
{
	public class PieceManagerSnapshot
	{
		[JsonConstructor]
		public PieceManagerSnapshot(int totalPieces, List<bool> localBitfield)
		{
			TotalPieces = totalPieces;
			LocalBitfield = localBitfield;
		}

		public PieceManagerSnapshot()
		{
		}

		public int TotalPieces { get; set; }
		public List<bool> LocalBitfield { get; set; } = new();
	}
}