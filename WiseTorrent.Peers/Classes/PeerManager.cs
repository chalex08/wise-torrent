using System.Collections.Concurrent;
using System.Net;
using WiseTorrent.Peers.Interfaces;
using WiseTorrent.Peers.Types;

namespace WiseTorrent.Peers.Classes
{
    internal class PeerManager : IPeerManager
    {
        private readonly ConcurrentDictionary<string, Peer> _connectedPeers = new();
        private readonly ConcurrentBag<IPEndPoint> _knownPeers = new();

        public void AddPeer(IPEndPoint peerEndpoint)
        {
            if (!_knownPeers.Contains(peerEndpoint))
            {
                _knownPeers.Add(peerEndpoint);
            }
        }

        public void ConnectToPeers()
        {
            foreach (var endpoint in _knownPeers)
            {
                string peerId = Guid.NewGuid().ToString();
                var peer = new Peer ()
                {
                    PeerID = peerId,
                    IPEndPoint = endpoint,
                    IsConnected = true,
                    LastActive = DateTime.UtcNow
                };
                _connectedPeers[peerId] = peer;
            }
        }

        public void DisconnectAll()
        {
            foreach (var peer in _connectedPeers.Values)
            {
                peer.IsConnected = false;
            }
            _connectedPeers.Clear();
        }

        public List<Peer> GetConnectedPeers()
        {
            return _connectedPeers.Values.Where(p => p.IsConnected).ToList();
        }

        public void RemovePeer(string peerId)
        {
            if (_connectedPeers.TryRemove(peerId, out var peer))
            {
                peer.IsConnected = false;
            }
        }

        public void MarkPeerActive(string peerId)
        {
            if (_connectedPeers.TryGetValue(peerId, out var peer))
            {
                peer.LastActive = DateTime.UtcNow;
            }
        }

        public void UpdatePeerStates()
        {
            foreach (var kvp in _connectedPeers)
            {
                var peer = kvp.Value;
                TimeSpan idleTime = DateTime.UtcNow - peer.LastActive;
                if (idleTime.TotalMinutes > 5)
                {
                    peer.IsConnected = false;
                    // Optionally remove or deprioritize peer
                }
            }
        }
    }
}
