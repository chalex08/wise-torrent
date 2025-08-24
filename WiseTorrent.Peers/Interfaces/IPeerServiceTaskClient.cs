using WiseTorrent.Utilities.Types;

namespace WiseTorrent.Peers.Interfaces
{
	public interface IPeerServiceTaskClient
	{
		Task StartServiceTask(TorrentSession torrentSession, CancellationToken cToken);
	}
}