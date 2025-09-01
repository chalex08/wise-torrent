using WiseTorrent.Utilities.Types;

namespace WiseTorrent.Core.Interfaces
{
	public interface ITorrentSessionManager
	{
		void AddSession(TorrentSession session);
		void RemoveSession(TorrentSession session);
		TorrentSession? GetSession(string torrentName);
		IEnumerable<TorrentSession> AllSessions { get; }
	}
}
