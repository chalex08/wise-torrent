using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using WiseTorrent.Peers.Interfaces;
using WiseTorrent.Peers.Types;

namespace WiseTorrent.Peers.Classes
{
    internal class PeerManager : IPeerManager
    {
        private List<Peer> _knownPeers; 

        private readonly byte[] _infoHash;
        private readonly string _localPeerId;

        public PeerManager(byte[] infoHash, string localPeerId)
        {
            _infoHash = infoHash;
            _localPeerId = localPeerId;
        }

        // main method for starting peer connection
        public async Task HandleTrackerResponse(List<Peer> peers)
        {
            if (peers.Count == 0) return;
            
            _knownPeers = peers;
            await ConnectToAllPeersAsync();

            // subscribe to torrentSession OnPeerManagerResponse event
            // invoke OnPeerManagerResponse(_activePeers) event
        }

        /// <summary>
        /// attempts to establish tcp connection which every peer
        /// if connection is successful = peer is added to the _connectedPeer list
        /// </summary>
        public async Task ConnectToAllPeersAsync()
        {
            var cts = new CancellationTokenSource();
            var tasks = _knownPeers.Select(peer => ConnectToPeerAsync(peer, cts.Token));
            await Task.WhenAll(tasks);
        }

        public async Task ConnectToPeerAsync(Peer peer, CancellationToken token)
        {
            using var client = new TcpClient();
            try
            {
                await client.ConnectAsync(peer.IPEndPoint.Address, peer.IPEndPoint.Port, token);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"Connection to {peer.IPEndPoint.Address} was cancelled.");
            }
            peer.IsConnected = true;
        }

        /// <summary>
        /// attempts handshake by sending a interested protocol, if:
        /// response = unchoked -> add to interestedPeer list
        /// response = choked -> return
        /// </summary>
        /// 

        public void DisconnectAll()
        {
            foreach (var peer in _knownPeers)
            {
                peer.IsConnected = false;
            }
            _knownPeers.Clear();
        }

        public List<Peer> GetConnectedPeers()
        {
            return _knownPeers.Where(p => p.IsConnected).ToList();
        }

        public void RemovePeer(string peerId)
        {
            var peer = _knownPeers.Where(p => p.PeerID == peerId).First();
            
            peer.IsConnected = false;
            peer.LastActive = DateTime.UtcNow;
        }

        public void MarkPeerActive(string peerId)
        {
            var peer = _knownPeers.Where(p => p.PeerID == peerId).First();
            peer.LastActive = DateTime.UtcNow;
        }

        public void UpdatePeerStates()
        {
            foreach (var kvp in _knownPeers)
            {
                var peer = kvp;
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
