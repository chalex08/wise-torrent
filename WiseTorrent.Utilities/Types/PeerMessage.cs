using System.Buffers.Binary;
using System.Text;

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
		public bool IsCanceled { get; set; } = false;

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
			const int payloadLength = 12; // piece index + offset + length
			var payload = new byte[payloadLength];

			// Piece index
			WriteInt(payload, 0, block.PieceIndex);

			// Block offset
			WriteInt(payload, 4, block.Offset);

			// Block length
			WriteInt(payload, 8, block.Length);

			return new PeerMessage(PeerMessageType.Request, payload);
		}

		public static PeerMessage CreatePieceMessage(Block block)
		{
			int dataLength = block.Data!.Length;
			var payload = new byte[4 + 4 + dataLength]; // index + offset + data

			// Piece index
			WriteInt(payload, 0, block.PieceIndex);

			// Block offset
			WriteInt(payload, 4, block.Offset);

			// Block data
			Buffer.BlockCopy(block.Data, 0, payload, 8, dataLength);

			return new PeerMessage(PeerMessageType.Piece, payload);
		}

		public static PeerMessage CreateHaveMessage(int pieceIndex)
		{
			var payload = new byte[4]; // piece index
			WriteInt(payload, 0, pieceIndex);

			return new PeerMessage(PeerMessageType.Have, payload);
		}

		private static void WriteInt(byte[] buffer, int offset, int value)
		{
			BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(offset, 4), value);
		}

		// For debugging/logging
		public override string ToString()
		{
			return $"Message: {MessageType}, Payload Length: {Payload.Length}";
		}
	}
}
