using System.Net;
using System.Net.Sockets;
using WiseTorrent.Utilities.Types;

namespace WiseTorrent.Tests.PeersTests
{
	[TestFixture]
	public class PeersTests
	{
		[Test]
		public void Constructor_InitializesPropertiesCorrectly()
		{
			var ip = IPAddress.Parse("192.168.1.100");
			var port = 6881;
			var peerID = "peer123";

			var peer = new Peer { IPEndPoint = new IPEndPoint(ip, port), PeerID = peerID };

			Assert.That(ip, Is.EqualTo(peer.IPEndPoint.Address));
			Assert.That(port, Is.EqualTo(peer.IPEndPoint.Port));
			Assert.That(peerID, Is.EqualTo(peer.PeerID));
			Assert.IsTrue(peer.IsChoked);
			Assert.IsFalse(peer.IsConnected);
			Assert.IsFalse(peer.IsInterested);
		}

		[Test]
		public void ToString_ReturnsExpectedFormat()
		{
			var peer = new Peer 
			{
				IPEndPoint = new IPEndPoint(IPAddress.Parse("10.0.0.1"), 1234),
				PeerID = "peerABC"
			};
			var result = peer.ToString();
			Assert.That(result, Is.EqualTo("10.0.0.1:1234 - ID: peerABC - Connected: False - Score: 50.00"));
		}

		[Test]
		public void AvailablePieces_CanAddAndTrackPieces()
		{
			var peer = new Peer
			{
				IPEndPoint = new IPEndPoint(IPAddress.Loopback, 6881),
				PeerID = "peerXYZ"
			};
			peer.AvailablePieces.Add(5);
			peer.AvailablePieces.Add(10);

			Assert.IsTrue(peer.AvailablePieces.Contains(5));
			Assert.IsTrue(peer.AvailablePieces.Contains(10));
			Assert.That(peer.AvailablePieces.Count, Is.EqualTo(2));
		}
	}
}
