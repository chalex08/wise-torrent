namespace WiseTorrent.Utilities.Types
{
	public class Block
	{
		public int PieceIndex { get; }
		public int Offset { get; }
		public int Length { get; }
		public byte[]? Data { get; set; }

		public Block(int pieceIndex, int offset, int length)
		{
			PieceIndex = pieceIndex;
			Offset = offset;
			Length = length;
		}

	}
}
