using WiseTorrent.Trackers.Types;
using WiseTorrent.Trackers.Interfaces;

namespace WiseTorrent.Core
{
    public class TorrentSession
    {
		private readonly Func<PeerDiscoveryProtocol, ITrackerClient> _trackerSelector;

		public TorrentSession(Func<PeerDiscoveryProtocol, ITrackerClient> trackerSelector)
		{
			_trackerSelector = trackerSelector;
		}
	}
}
