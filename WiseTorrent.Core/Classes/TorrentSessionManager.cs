using System.Collections.Concurrent;
using WiseTorrent.Core.Interfaces;
using WiseTorrent.Utilities.Interfaces;
using WiseTorrent.Utilities.Types;

namespace WiseTorrent.Core.Classes
{
	internal class TorrentSessionManager : ITorrentSessionManager
	{
		private readonly ILogger<TorrentSessionManager> _logger;

		public TorrentSessionManager(ILogger<TorrentSessionManager> logger)
		{
			_logger = logger;
		}

		private readonly ConcurrentDictionary<byte[], TorrentSession> _sessions = new();

		public void AddSession(TorrentSession session)
		{
			_sessions[session.InfoHash] = session;
			_logger.Info($"Added new torrent session (Torrent Name: {session.Info.Name})");
		}

		public TorrentSession? GetSession(byte[] infoHash)
		{
			return _sessions.GetValueOrDefault(infoHash);
		}

		public IEnumerable<TorrentSession> AllSessions => _sessions.Values;
	}
}