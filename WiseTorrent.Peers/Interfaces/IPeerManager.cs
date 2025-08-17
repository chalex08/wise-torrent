using System.Net;
using WiseTorrent.Peers.Types;

namespace WiseTorrent.Peers.Interfaces
{
	public interface IPeerManager
	{
        Task HandleTrackerResponse(List<Peer> peers);
        Task ConnectToPeerAsync(Peer peer, CancellationToken token);
        void DisconnectAll();
        List<Peer> GetConnectedPeers();
        void RemovePeer(string peerId);
        void MarkPeerActive(string peerId);
        void UpdatePeerStates();
    }
}
