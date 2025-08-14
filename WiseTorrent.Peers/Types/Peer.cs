using System.Text;

namespace WiseTorrent.Peers.Types
{
	public class Peer
	{
		public required string IP;
		public required int Port;
		public string? PeerID;
		public byte[] PeerIDBytes => Encoding.UTF8.GetBytes(PeerID);
	}
}
