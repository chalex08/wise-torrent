using System.Security.Cryptography;

namespace WiseTorrent.Utilities.Types
{
	public class Piece
	{
		public int Index { get; }
		public byte[] ExpectedHash { get; }
		public bool State { get; private set; }
		public int Length => Blocks.Sum(b => b.Data?.Length ?? 0);
		public IEnumerable<Block> Blocks { get; private set; }

		public int DownloadAttempts { get; private set; }
		public int ValidationFailures { get; private set; }
		public DateTime? LastValidationTime { get; private set; }

		public Piece(int index, byte[] expectedHash, int pieceLength)
		{
			Index = index;
			ExpectedHash = expectedHash;
			State = false;
			Blocks = SplitPieceToBlocks(index, pieceLength).ToList();
		}

		public static IEnumerable<Block> SplitPieceToBlocks(int pieceIndex, int pieceLength)
		{
			for (int offset = 0; offset < pieceLength; offset += SessionConfig.BlockSizeBytes)
			{
				int length = Math.Min(SessionConfig.BlockSizeBytes, pieceLength - offset);
				yield return new Block(pieceIndex, offset, length);
			}
		}

		private bool IsPieceValid()
		{
			if (Blocks.Any(b => b.Data == null)) return false;
			var assembled = Blocks.SelectMany(b => b.Data!).ToArray();
			return SHA1.HashData(assembled).SequenceEqual(ExpectedHash);
		}

		public bool IsBlockValid(Block block)
		{
			if (block.PieceIndex != Index)
				return false;

			var expected = Blocks.FirstOrDefault(b => b.Offset == block.Offset && b.Length == block.Length);
			if (expected == null)
				return false;

			return block.Data != null && block.Data.Length == block.Length;
		}

		public bool IsPieceComplete()
		{
			return IsPieceValid();
		}

		public static bool ArePiecesEqual(Piece piece1, Piece piece2)
		{
			return piece1.Index == piece2.Index && piece1.Length == piece2.Length && piece1.ExpectedHash.SequenceEqual(piece2.ExpectedHash);
		}
	}
}