using System.Text;

namespace WiseTorrent.Utilities.Types
{
	public class HandshakeMessage
	{
		public HandshakeMessage(byte[] infoHash, string peerId)
		{
			InfoHash = infoHash; 
			PeerId = peerId;
		}

		public byte[] InfoHash { get; init; }
		public string PeerId { get; init; }

		private const string ProtocolString = "BitTorrent protocol";
		private const int HandshakeLength = 68;

		public byte[] ToBytes()
		{
			byte[] handshake = new byte[HandshakeLength];

			// pstrlen (1 byte)
			handshake[0] = (byte)ProtocolString.Length;

			// pstr (19 bytes)
			Encoding.ASCII.GetBytes(ProtocolString).CopyTo(handshake, 1);

			// reserved (8 bytes, all zero)
			for (int i = 20; i < 28; i++) handshake[i] = 0;

			// info_hash (20 bytes)
			if (InfoHash.Length != 20)
				throw new ArgumentException("InfoHash must be 20 bytes.");

			InfoHash.CopyTo(handshake, 28);

			// peer_id (20 bytes)
			byte[] peerIdBytes = Encoding.ASCII.GetBytes(PeerId ?? "");
			if (peerIdBytes.Length != 20 && PeerId != null)
				throw new ArgumentException("PeerID must be 20 bytes.");

			peerIdBytes.CopyTo(handshake, 48);

			return handshake;
		}

		public static HandshakeMessage? FromBytes(byte[] data)
		{
			if (data.Length != HandshakeLength || data[0] != 19) return null;
			var protocol = Encoding.ASCII.GetString(data, 1, 19);
			if (protocol != ProtocolString) return null;

			var infoHash = data.Skip(28).Take(20).ToArray();
			var peerId = Encoding.ASCII.GetString(data.Skip(48).Take(20).ToArray());

			return new HandshakeMessage(infoHash, peerId);
		}
	}
}