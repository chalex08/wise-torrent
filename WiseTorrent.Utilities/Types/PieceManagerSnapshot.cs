namespace WiseTorrent.Utilities.Types
{
	public class PieceManagerSnapshot
	{
		public int TotalPieces { get; set; }
		public List<bool> LocalBitfield { get; set; } = new();
	}
}
