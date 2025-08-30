using WiseTorrent.Utilities.Types;

namespace WiseTorrent.Trackers.Interfaces
{
	public interface ITrackerServiceTaskClient
	{
		Task StartServiceTask(TorrentSession torrentSession, CancellationToken cToken);
	}
}
