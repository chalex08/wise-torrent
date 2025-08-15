using WiseTorrent.Utilities.Types;

namespace WiseTorrent.Parsing.Types
{
	public class TrackerResponse(int interval, List<Peer> peers)
	{
		public int Interval = interval;
		public List<Peer> Peers = peers;
	}
}
