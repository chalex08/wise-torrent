using System.Collections.Concurrent;
using WiseTorrent.Utilities.Interfaces;
using WiseTorrent.Utilities.Types;

namespace WiseTorrent.Core
{
	public class TorrentSessionManager
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
		}

		public TorrentSession? GetSession(byte[] infoHash)
		{
			return _sessions.GetValueOrDefault(infoHash);
		}

		public IEnumerable<TorrentSession> AllSessions => _sessions.Values;
	}
}