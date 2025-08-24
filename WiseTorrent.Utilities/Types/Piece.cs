using System.Security.Cryptography;

namespace WiseTorrent.Utilities.Types
{
	public class Piece
	{
		public int Index { get; }
		public byte[] ExpectedHash { get; }
		public byte[]? Data { get; private set; }
		public bool State { get; private set; }
		public int Length => Data?.Length ?? 0;
		public IEnumerable<Block> Blocks { get; private set; }

		public int DownloadAttempts { get; private set; }
		public int ValidationFailures { get; private set; }
		public DateTime? LastValidationTime { get; private set; }

		public Piece(int index, byte[] expectedHash, int pieceLength)
		{
			Index = index;
			ExpectedHash = expectedHash;
			State = false;
			Blocks = SplitPiece(index, pieceLength);
		}

		private IEnumerable<Block> SplitPiece(int pieceIndex, int pieceLength)
		{
			for (int offset = 0; offset < pieceLength; offset += SessionConfig.BlockSizeBytes)
			{
				int length = Math.Min(SessionConfig.BlockSizeBytes, pieceLength - offset);
				yield return new Block(pieceIndex, offset, length);
			}
		}


		public void SetData(byte[] data)
		{
			if (State) return; // prevent overwriting validated data
			Data = data;
			DownloadAttempts++;
		}

		private bool IsValid()
		{
			if (Data == null) return false;
			return SHA1.HashData(Data).SequenceEqual(ExpectedHash);
		}

		public void Validate()
		{
			if (Data == null)
			{
				State = false;
				return;
			}

			LastValidationTime = DateTime.UtcNow;
			State = IsValid();

			if (!State)
				ValidationFailures++;
		}
	}
}