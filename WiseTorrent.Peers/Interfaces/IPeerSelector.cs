using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WiseTorrent.Peers.Types;

namespace WiseTorrent.Peers.Interfaces
{
    internal interface IPeerSelector
    {
        List<Peer> SelectUnchokePeers(List<Peer> connectedPeers, int maxUnchokeCount);
        Peer? SelectOptimisticUnchoke(List<Peer> connectedPeers, HashSet<Peer> excludedPeers);
        int ScorePeer(Peer peer);
    }
}
