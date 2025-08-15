using System.Net;
using WiseTorrent.Peers.Types;

namespace WiseTorrent.Peers.Interfaces
{
	public interface IPeerManager
	{
        void AddPeer(IPEndPoint peerEndpoint);
        void ConnectToPeers();
        void DisconnectAll();
        List<Peer> GetConnectedPeers();
        void RemovePeer(string peerId);
        void MarkPeerActive(string peerId);
        void UpdatePeerStates();
    }
}
