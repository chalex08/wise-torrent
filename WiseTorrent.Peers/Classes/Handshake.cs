using System.Text;
using WiseTorrent.Peers.Interfaces;

namespace WiseTorrent.Peers.Classes
{
	internal class Handshake : IHandshake
	{
		private const string ProtocolString = "BitTorrent protocol";
		private const int HandshakeLength = 68;

		public byte[] CreateHandshake(byte[] infoHash, string peerId)
		{
			byte[] handshake = new byte[HandshakeLength];

			// pstrlen (1 byte)
			handshake[0] = (byte)ProtocolString.Length;

			// pstr (19 bytes)
			Encoding.ASCII.GetBytes(ProtocolString).CopyTo(handshake, 1);

			// reserved (8 bytes, all zero)
			for (int i = 20; i < 28; i++) handshake[i] = 0;

			// info_hash (20 bytes)
			if (infoHash.Length != 20)
				throw new ArgumentException("InfoHash must be 20 bytes.");

			infoHash.CopyTo(handshake, 28);

			// peer_id (20 bytes)
			byte[] peerIdBytes = Encoding.ASCII.GetBytes(peerId);
			if (peerIdBytes.Length != 20)
				throw new ArgumentException("PeerID must be 20 bytes.");

			peerIdBytes.CopyTo(handshake, 48);

			return handshake;
		}

		public bool IsValidHandshake(byte[] receivedInfoHash, byte[] expectedInfoHash)
		{
			return receivedInfoHash.SequenceEqual(expectedInfoHash);
		}

		public bool TryParseHandshake(byte[] data, out byte[] infoHash, out string peerId)
		{
			infoHash = new byte[20];
			peerId = string.Empty;

			if (data.Length != HandshakeLength) return false;

			// Extract info_hash (bytes 28–47)
			Array.Copy(data, 28, infoHash, 0, 20);

			// Extract peer_id (bytes 48–67)
			byte[] peerIdBytes = new byte[20];
			Array.Copy(data, 48, peerIdBytes, 0, 20);
			peerId = Encoding.ASCII.GetString(peerIdBytes);

			return true;
		}
	}
}
