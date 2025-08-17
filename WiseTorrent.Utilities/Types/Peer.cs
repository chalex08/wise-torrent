using System.Net;
using System.Text;

namespace WiseTorrent.Utilities.Types
{
	public class Peer
	{
		public string? PeerID { get; set; }
		public required IPEndPoint IPEndPoint { get; set; }
		public byte[] PeerIDBytes => PeerID != null ? Encoding.ASCII.GetBytes(PeerID) : [];
	}
}
