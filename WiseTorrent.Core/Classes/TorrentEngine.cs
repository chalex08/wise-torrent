using WiseTorrent.Core.Interfaces;
using WiseTorrent.Parsing.Interfaces;
using WiseTorrent.Peers.Interfaces;
using WiseTorrent.Storage.Interfaces;
using WiseTorrent.Trackers.Interfaces;
using WiseTorrent.Utilities.Interfaces;
using WiseTorrent.Utilities.Types;

namespace WiseTorrent.Core.Classes
{
	internal class TorrentEngine : ITorrentEngine
	{
		private readonly ILogger<TorrentEngine> _logger;
		private readonly ITorrentSessionManager _torrentSessionManager;
		private readonly ITorrentParser _parser;
		private readonly ITrackerServiceTaskClient _trackerServiceTaskClient;
		private readonly IPeerServiceTaskClient _peerServiceTaskClient;
		private readonly IStorageServiceTaskClient _storageServiceTaskClient;

		public TorrentEngine(ILogger<TorrentEngine> logger, ITorrentSessionManager torrentSessionManager, ITorrentParser parser,
			ITrackerServiceTaskClient trackerServiceTaskClient, IPeerServiceTaskClient peerServiceTaskClient, IStorageServiceTaskClient storageServiceTaskClient)
		{
			_logger = logger;
			_torrentSessionManager = torrentSessionManager;
			_parser = parser;
			_trackerServiceTaskClient = trackerServiceTaskClient;
			_peerServiceTaskClient = peerServiceTaskClient;
			_storageServiceTaskClient = storageServiceTaskClient;
		}

		public async Task CancelTorrentEngineSession(string filePath)
		{
			var torrentSession = _torrentSessionManager.GetSession(filePath.Split(Path.DirectorySeparatorChar).Last());
			if (torrentSession != null)
			{
				_logger.Info($"Cancelling torrent session (Torrent Name: {torrentSession.Info.Name})");
				await torrentSession.Cts.CancelAsync();
				_torrentSessionManager.RemoveSession(torrentSession);
				_logger.Info($"Successfully cancelled torrent session (Torrent Name: {torrentSession.Info.Name})");
			}
		}

		public void StartTorrentEngineSession(string filePath)
		{
			var torrentSession = StartParsingPhase(filePath);
			if (torrentSession == null) return;

			_torrentSessionManager.AddSession(torrentSession);
			var currentSessionToken = torrentSession.Cts.Token;

			StartTrackerPhase(torrentSession, currentSessionToken);

			bool downloadStarted = false;

			void TrackerListener(ConcurrentSet<Peer> peers)
			{
				if (downloadStarted || !peers.Any()) return;
				downloadStarted = true;

				torrentSession.AllPeers = peers;
				StartDownloadPhase(torrentSession, currentSessionToken);
				torrentSession.OnTrackerResponse.Unsubscribe(TrackerListener);
			}

			torrentSession.OnTrackerResponse.Subscribe(TrackerListener);
		}

		private TorrentSession? StartParsingPhase(string filePath)
		{
			TorrentMetadata? parsedMetadata = _parser.ParseTorrentFileFromPath(filePath);
			if (parsedMetadata == null)
			{
				_logger.Error("Parsed torrent metadata was null");
				return null;
			}

			TorrentSession session = TorrentSession.CreateSessionFromMetadata(parsedMetadata);
			return session;
		}

		private void StartTrackerPhase(TorrentSession torrentSession, CancellationToken cToken)
		{
			_ = Task.Run(async () =>
			{
				try
				{
					await _trackerServiceTaskClient.StartServiceTask(torrentSession, cToken);
				}
				catch (OperationCanceledException)
				{
					_logger.Info("Tracker service task canceled cleanly");
				}
				catch (Exception ex)
				{
					_logger.Error("Tracker service task failed", ex);
				}
			}, cToken);
			_logger.Info($"Tracker service task started (Torrent Name: {torrentSession.Info.Name})");
		}

		private void StartDownloadPhase(TorrentSession torrentSession, CancellationToken cToken)
		{
			_ = Task.Run(async () =>
			{
				try
				{
					await _peerServiceTaskClient.StartServiceTask(torrentSession, cToken);
				}
				catch (OperationCanceledException)
				{
					_logger.Info("Peer service task canceled cleanly");
				}
				catch (Exception ex)
				{
					_logger.Error("Peer service task failed", ex);
				}
			}, cToken);
			_logger.Info($"Peer service task started (Torrent Name: {torrentSession.Info.Name})");

			_ = Task.Run(async () =>
			{
				try
				{
					await _storageServiceTaskClient.StartServiceTask(torrentSession, cToken);
				}
				catch (OperationCanceledException)
				{
					_logger.Info("Storage service task canceled cleanly");
				}
				catch (Exception ex)
				{
					_logger.Error("Storage service task failed", ex);
				}
			}, cToken);
			_logger.Info($"Storage service task started (Torrent Name: {torrentSession.Info.Name})");
		}
	}
}
