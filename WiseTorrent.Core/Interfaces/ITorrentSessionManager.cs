using WiseTorrent.Utilities.Types;

namespace WiseTorrent.Core.Interfaces
{
	public interface ITorrentSessionManager
	{
		void AddSession(TorrentSession session);
		TorrentSession? GetSession(byte[] infoHash);
		IEnumerable<TorrentSession> AllSessions { get; }
	}
}
