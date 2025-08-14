using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WiseTorrent.Peers.Interfaces;
using WiseTorrent.Peers.Types;

namespace WiseTorrent.Peers.Classes
{
    internal class PeerSelector : IPeerSelector
    {
        public int ScorePeer(Peer peer)
        {
            throw new NotImplementedException();
        }

        public Peer? SelectOptimisticUnchoke(List<Peer> connectedPeers, HashSet<Peer> excludedPeers)
        {
            throw new NotImplementedException();
        }

        public List<Peer> SelectUnchokePeers(List<Peer> connectedPeers, int maxUnchokeCount)
        {
            throw new NotImplementedException();
        }
    }
}
