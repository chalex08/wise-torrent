using WiseTorrent.Utilities.Types;

namespace WiseTorrent.Utilities.Interfaces
{
	public interface IServiceTaskClient
	{
		Task StartServiceTask(TorrentSession torrentSession, CancellationToken cToken);
	}
}
