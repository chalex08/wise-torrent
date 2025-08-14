using WiseTorrent.Trackers.Interfaces;

namespace WiseTorrent.Core
{
    public class TorrentSession
    {
		private readonly ITrackerServiceTaskClient _trackerServiceTaskClient;

		public TorrentSession(ITrackerServiceTaskClient trackerServiceTaskClient)
		{
			_trackerServiceTaskClient = trackerServiceTaskClient;
		}
	}
}
