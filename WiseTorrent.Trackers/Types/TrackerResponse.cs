using WiseTorrent.Peers.Types;

namespace WiseTorrent.Trackers.Types
{
    internal class TrackerResponse
    {
	    public int Interval;
        public required List<Peer> Peers;
    }
}
