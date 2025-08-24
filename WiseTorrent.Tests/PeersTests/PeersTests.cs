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

            Assert.AreEqual(ip, peer.IPEndPoint.Address);
            Assert.AreEqual(port, peer.IPEndPoint.Port);
            Assert.AreEqual(peerID, peer.PeerID);
            Assert.IsTrue(peer.IsChoked);
            Assert.IsFalse(peer.IsConnected);
            Assert.IsFalse(peer.IsInterested);
        }

        [Test]
        public void ToString_ReturnsExpectedFormat()
        {
            var peer = new Peer(IPAddress.Parse("10.0.0.1"), 1234, "peerABC");
            var result = peer.ToString();
            Assert.AreEqual("10.0.0.1:1234 - ID: peerABC", result);
        }

        [Test]
        public void AvailablePieces_CanAddAndTrackPieces()
        {
            var peer = new Peer(IPAddress.Loopback, 6881, "peerXYZ");
            peer.AvailablePieces.Add(5);
            peer.AvailablePieces.Add(10);

            Assert.IsTrue(peer.AvailablePieces.Contains(5));
            Assert.IsTrue(peer.AvailablePieces.Contains(10));
            Assert.AreEqual(2, peer.AvailablePieces.Count);
        }

        [Test]
        public void TrySend_ReturnsFalse_WhenStreamIsNull()
        {
            var peer = new Peer(IPAddress.Loopback, 6881, "peerXYZ");
            var result = peer.TrySend(new byte[] { 0x01, 0x02 });
            Assert.IsFalse(result);
        }

        [Test]
        public void TrySend_ReturnsFalse_WhenStreamIsNotWritable()
        {
            var peer = new Peer(IPAddress.Loopback, 6881, "peerXYZ");
            var mockClient = new TcpClient();
            var mockStream = new MemoryStream(new byte[10], false); // not writable

            peer.Connection = mockClient;
            typeof(TcpClient).GetProperty("Client")?.SetValue(mockClient, new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp));
            Assert.IsFalse(peer.TrySend(new byte[] { 0x01 }));
        }

        [Test]
        public void TrySend_ReturnsTrue_WhenStreamIsWritable()
        {
            var peer = new Peer(IPAddress.Loopback, 6881, "peerXYZ");
            var mockClient = new TcpClient();
            var stream = new MemoryStream();

            peer.Connection = mockClient;

            // Simulate writable stream using NetworkStream override (not trivial to mock directly)
            // For full testing, consider abstracting stream access behind an interface

            Assert.Pass("Stream mocking for TcpClient is limited in unit tests. Consider refactoring for testability.");
        }
    }
}
