namespace WiseTorrent.Utilities.Types
{
	public class Block : IEquatable<Block>
	{
		public int PieceIndex { get; }
		public int Offset { get; }
		public int Length { get; }
		public byte[]? Data { get; set; }
		public bool IsMarkedForRetry { get; set; } = false;

		public Block() { } // needed for deserialization

		public Block(int pieceIndex, int offset, int length)
		{
			PieceIndex = pieceIndex;
			Offset = offset;
			Length = length;
		}

		public bool Equals(Block? other)
		{
			if (other is null) return false;
			return PieceIndex == other.PieceIndex &&
				   Offset == other.Offset &&
				   Length == other.Length;
		}

		public override bool Equals(object? obj) => Equals(obj as Block);

		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = hash * 31 + PieceIndex;
				hash = hash * 31 + Offset;
				hash = hash * 31 + Length;
				return hash;
			}
		}

		public static Block? ParseRequestMessage(byte[] rawMessage)
		{
			if (rawMessage.Length != 17)
				return null; // Invalid length for a request message

			int lengthPrefix = ReadInt(rawMessage, 0);
			if (lengthPrefix != 13)
				return null; // Request message must have length prefix of 13

			if (rawMessage[4] != 6)
				return null; // Not a request message

			int pieceIndex = ReadInt(rawMessage, 5);
			int offset = ReadInt(rawMessage, 9);
			int blockLength = ReadInt(rawMessage, 13);

			return new Block(pieceIndex, offset, blockLength);
		}

		public static Block? ParsePieceMessage(byte[] rawMessage)
		{
			if (rawMessage.Length < 13)
				return null; // Too short to be a valid piece message

			int lengthPrefix = ReadInt(rawMessage, 0);
			if (lengthPrefix != rawMessage.Length - 4)
				return null; // Mismatched length prefix

			if (rawMessage[4] != 7)
				return null; // Not a piece message

			int pieceIndex = ReadInt(rawMessage, 5);
			int offset = ReadInt(rawMessage, 9);

			int dataLength = rawMessage.Length - 13;
			if (dataLength <= 0)
				return null; // No block data

			byte[] blockData = new byte[dataLength];
			Buffer.BlockCopy(rawMessage, 13, blockData, 0, dataLength);

			var block = new Block(pieceIndex, offset, dataLength)
			{
				Data = blockData
			};

			return block;
		}

		public static Block? ParseCancelMessage(byte[] rawMessage)
		{
			if (rawMessage.Length != 17)
				return null; // Invalid length for a cancel message

			int lengthPrefix = ReadInt(rawMessage, 0);
			if (lengthPrefix != 13)
				return null; // Cancel message must have length prefix of 13

			if (rawMessage[4] != 8)
				return null; // Not a cancel message

			int pieceIndex = ReadInt(rawMessage, 5);
			int offset = ReadInt(rawMessage, 9);
			int blockLength = ReadInt(rawMessage, 13);

			return new Block(pieceIndex, offset, blockLength);
		}

		private static int ReadInt(byte[] buffer, int offset)
		{
			return (buffer[offset] << 24) |
				   (buffer[offset + 1] << 16) |
				   (buffer[offset + 2] << 8) |
				   buffer[offset + 3];
		}

		public static bool AreBlocksEqual(Block block1, Block block2)
		{
			return block1.Offset == block2.Offset && block1.Length == block2.Length && block1.PieceIndex == block2.PieceIndex;
		}

		public static bool AreBlocksEqual(byte[] payload, Block block)
		{
			if (payload.Length != 12)
				return false;

			int pieceIndex = ReadInt(payload, 0);
			int offset = ReadInt(payload, 4);
			int length = ReadInt(payload, 8);

			return pieceIndex == block.PieceIndex &&
				   offset == block.Offset &&
				   length == block.Length;
		}
	}
}
