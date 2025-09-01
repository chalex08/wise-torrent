using System.Collections.Concurrent;
using WiseTorrent.Core.Interfaces;
using WiseTorrent.Utilities.Interfaces;
using WiseTorrent.Utilities.Types;

namespace WiseTorrent.Core.Classes
{
    public class TorrentSessionManager : ITorrentSessionManager
	{
		private readonly ILogger<TorrentSessionManager> _logger;

		public TorrentSessionManager(ILogger<TorrentSessionManager> logger)
		{
			_logger = logger;
		}

		private readonly ConcurrentDictionary<string, TorrentSession> _sessions = new();

		public void AddSession(TorrentSession session)
		{
			_sessions[session.Info.Name] = session;
			_logger.Info($"Added new torrent session (Torrent Name: {session.Info.Name})");
		}

		public void RemoveSession(TorrentSession session)
		{
			_sessions.Remove(session.Info.Name, out _);
			_logger.Info($"Removed torrent session (Torrent Name: {session.Info.Name})");
		}

		public TorrentSession? GetSession(string torrentName)
		{
			return _sessions.GetValueOrDefault(torrentName);
		}

		public IEnumerable<TorrentSession> AllSessions => _sessions.Values;
	}
}