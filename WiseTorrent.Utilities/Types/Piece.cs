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
			Blocks = SplitPieceToBlocks(index, pieceLength);
		}

		public static IEnumerable<Block> SplitPieceToBlocks(int pieceIndex, int pieceLength)
		{
			for (int offset = 0; offset < pieceLength; offset += SessionConfig.BlockSizeBytes)
			{
				int length = Math.Min(SessionConfig.BlockSizeBytes, pieceLength - offset);
				yield return new Block(pieceIndex, offset, length);
			}
		}

		private bool IsValid()
		{
			if (Blocks.Any(b => b.Data == null)) return false;
			return SHA1.HashData((byte[])Blocks.SelectMany(b => b.Data!)).SequenceEqual(ExpectedHash);
		}

		public void Validate()
		{
			if (Blocks.Any(b => b.Data == null))
			{
				State = false;
				return;
			}

			LastValidationTime = DateTime.UtcNow;
			State = IsValid();

			if (!State)
				ValidationFailures++;
		}

		public bool IsPieceComplete()
		{
			return Blocks.All(b => b.Data != null);
		}
	}
}