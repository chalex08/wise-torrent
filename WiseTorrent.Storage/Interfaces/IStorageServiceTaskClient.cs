using WiseTorrent.Utilities.Types;

namespace WiseTorrent.Storage.Interfaces
{
	public interface IStorageServiceTaskClient
	{
		Task StartServiceTask(TorrentSession torrentSession, CancellationToken cToken);
	}
}
