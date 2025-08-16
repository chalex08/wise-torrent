using WiseTorrent.Utilities.Types;

namespace WiseTorrent.Trackers.Interfaces
{
	public interface ITrackerClient
	{
		Task<bool> RunServiceTask(TorrentSession torrentSession, CancellationToken cancellationToken);
	}
}
