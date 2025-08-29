namespace WiseTorrent.Utilities.Types
{
   
	// Enum for known BitTorrent message types
	public enum PeerMessageType : byte
	{
		Choke = 0,
		Unchoke = 1,
		Interested = 2,
		NotInterested = 3,
		Have = 4,
		Bitfield = 5,
		Request = 6,
		Piece = 7,
		Cancel = 8,
		KeepAlive = 255 // Special case: no ID, length = 0
	}

	public class PeerMessage
	{
		// Type of the message
		public PeerMessageType MessageType { get; set; }
		public HandshakeMessage? HandshakeMessage { get; set; }

		// Optional payload data
		public byte[] Payload { get; set; } = Array.Empty<byte>();

		// Constructor
		public PeerMessage(PeerMessageType type, byte[]? payload = null)
		{
			MessageType = type;
			Payload = payload ?? Array.Empty<byte>();
		}

		public PeerMessage(HandshakeMessage handshakeMessage)
		{
			HandshakeMessage = handshakeMessage;
		}

		// Serializes the message into a byte array for sending
		public byte[] ToBytes()
		{
			using var ms = new MemoryStream();

			if (MessageType == PeerMessageType.KeepAlive)
			{
				// Keep-alive message: 4-byte length of zero
				ms.Write(BitConverter.GetBytes(0).Reverse().ToArray(), 0, 4);
			}
			else if (HandshakeMessage != null)
			{
				ms.Write(HandshakeMessage.ToBytes());
			}
			else
			{
				// Length = 1 byte for ID + payload length
				int length = 1 + Payload.Length;
				ms.Write(BitConverter.GetBytes(length).Reverse().ToArray(), 0, 4);
				ms.WriteByte((byte)MessageType);
				ms.Write(Payload, 0, Payload.Length);
			}

			return ms.ToArray();
		}

		// Parses a message from a byte array
		public static PeerMessage? FromBytes(byte[] data)
		{
			if (data.Length < 4) return null;

			int length = BitConverter.ToInt32(data.Take(4).Reverse().ToArray(), 0);

			if (length == 0)
			{
				// Keep-alive message
				return new PeerMessage(PeerMessageType.KeepAlive);
			}

			if (data.Length < 4 + length) return null;

			byte messageId = data[4];
			byte[] payload = data.Skip(5).Take(length - 1).ToArray();

			return Enum.IsDefined(typeof(PeerMessageType), messageId) ? new PeerMessage((PeerMessageType)messageId, payload) : null;
		}

		public static PeerMessage CreateKeepAlive()
		{
			return new PeerMessage(PeerMessageType.KeepAlive);
		}

		public static PeerMessage CreateRequestMessage(Block block)
		{
			const byte messageId = (byte)PeerMessageType.Request; // 'request' message
			const int payloadLength = 13; // piece index + offset + length

			var buffer = new byte[4 + payloadLength]; // total = 17 bytes

			// Length prefix (big-endian)
			WriteInt(buffer, 0, payloadLength);

			// Message ID
			buffer[4] = messageId;

			// Piece index
			WriteInt(buffer, 5, block.PieceIndex);

			// Block offset
			WriteInt(buffer, 9, block.Offset);

			// Block length
			WriteInt(buffer, 13, block.Length);

			return new PeerMessage(PeerMessageType.Request, buffer);
		}

		public static PeerMessage CreatePieceMessage(Block block)
		{
			const byte messageId = (byte)PeerMessageType.Piece; // 'piece' message ID
			int dataLength = block.Data!.Length;
			int payloadLength = 1 + 4 + 4 + dataLength; // ID + index + offset + data

			var buffer = new byte[4 + payloadLength]; // total = 4 (length prefix) + payload

			// Length prefix (big-endian)
			WriteInt(buffer, 0, payloadLength);

			// Message ID
			buffer[4] = messageId;

			// Piece index
			WriteInt(buffer, 5, block.PieceIndex);

			// Block offset
			WriteInt(buffer, 9, block.Offset);

			// Block data
			Buffer.BlockCopy(block.Data, 0, buffer, 13, dataLength);

			return new PeerMessage(PeerMessageType.Piece, buffer);
		}

		public static PeerMessage CreateHaveMessage(int pieceIndex)
		{
			const byte messageId = (byte)PeerMessageType.Have; // 'have' message ID
			const int payloadLength = 5; // 1 byte for ID + 4 bytes for piece index

			var buffer = new byte[4 + payloadLength]; // 4-byte length prefix + payload

			// Length prefix (big-endian)
			WriteInt(buffer, 0, payloadLength);

			// Message ID
			buffer[4] = messageId;

			// Piece index
			WriteInt(buffer, 5, pieceIndex);

			return new PeerMessage(PeerMessageType.Have, buffer);
		}

		private static void WriteInt(byte[] buffer, int offset, int value)
		{
			buffer[offset] = (byte)((value >> 24) & 0xFF);
			buffer[offset + 1] = (byte)((value >> 16) & 0xFF);
			buffer[offset + 2] = (byte)((value >> 8) & 0xFF);
			buffer[offset + 3] = (byte)(value & 0xFF);
		}

		// For debugging/logging
		public override string ToString()
		{
			return $"Message: {MessageType}, Payload Length: {Payload.Length}";
		}
	}
}
