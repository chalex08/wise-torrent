using System.IO;
using System.Text.Json;
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
			var torrentSession = _torrentSessionManager.GetSession(Path.GetFileNameWithoutExtension(filePath));
			if (torrentSession != null)
			{
				_logger.Info($"Cancelling torrent session (Torrent Name: {torrentSession.Info.Name})");
				torrentSession.ShouldFlushOnShutdown = false;
				torrentSession.ShouldSnapshotOnShutdown = false;
				await torrentSession.Cts.CancelAsync();
				_torrentSessionManager.RemoveSession(torrentSession);
				_logger.Info($"Successfully cancelled torrent session (Torrent Name: {torrentSession.Info.Name})");
			}
		}

		public async Task PauseTorrentEngineSession(string filePath)
		{
			var torrentSession = _torrentSessionManager.GetSession(Path.GetFileNameWithoutExtension(filePath));
			if (torrentSession != null)
			{
				_logger.Info($"Pausing torrent session (Torrent Name: {torrentSession.Info.Name})");
				torrentSession.ShouldFlushOnShutdown = true;
				torrentSession.ShouldSnapshotOnShutdown = true;
				await torrentSession.Cts.CancelAsync();
				_torrentSessionManager.RemoveSession(torrentSession);

				var tcs = new TaskCompletionSource();
				bool stored = false;
				bool piecesFlushed = false;
				PieceManagerSnapshot? pieceManagerSnapshot = null;
				torrentSession.OnPiecesFlushed.Subscribe(_ =>
				{
					piecesFlushed = true;
					if (pieceManagerSnapshot != null && !stored)
					{
						TryStoreTorrentSession(torrentSession, pieceManagerSnapshot, tcs);
						stored = true;
					}
				});
				torrentSession.OnPieceManagerSnapshotted.Subscribe(snapshot =>
				{
					if (piecesFlushed && !stored)
					{
						TryStoreTorrentSession(torrentSession, snapshot, tcs);
						stored = true;
					}
					else
					{
						pieceManagerSnapshot = snapshot;
					}
				});
				await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(10)));

				_logger.Info($"Successfully paused torrent session (Torrent Name: {torrentSession.Info.Name})");
			}
			else
			{
				_logger.Error($"Failed to pause torrent session (Torrent File Path: {filePath})");
			}
		}

		private void TryStoreTorrentSession(TorrentSession torrentSession, PieceManagerSnapshot? pieceManagerSnapshot, TaskCompletionSource tcs)
		{
			if (pieceManagerSnapshot == null) return;

			var sessionSnapshot = PausedTorrentSessionSnapshot.CreateSnapshotOfSession(torrentSession, pieceManagerSnapshot);
			var json = JsonSerializer.Serialize(sessionSnapshot, new JsonSerializerOptions { WriteIndented = true });
			string sessionDir = Path.Combine(AppContext.BaseDirectory, "PausedSessions");
			Directory.CreateDirectory(sessionDir); // ensures it exists

			string fileName = $"{torrentSession.Info.Name}.session.json";
			string fullPath = Path.Combine(sessionDir, fileName);

			File.WriteAllText(fullPath, json);
			tcs.TrySetResult();
		}

		public void StartTorrentEngineSession(string filePath)
		{
			TorrentSession? torrentSession = TryLoadPausedSession(filePath)
				?? StartParsingPhase(filePath);

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
			torrentSession.OnFileCompleted.Subscribe(async _ =>
			{
				await HandleFileCompletion(torrentSession);
			});
		}

		private TorrentSession? TryLoadPausedSession(string torrentFilePath)
		{
			string torrentName = Path.GetFileNameWithoutExtension(torrentFilePath);
			string sessionDir = Path.Combine(AppContext.BaseDirectory, "PausedSessions");
			string sessionPath = Path.Combine(sessionDir, $"{torrentName}.session.json");

			if (!File.Exists(sessionPath)) return null;

			try
			{
				string json = File.ReadAllText(sessionPath);
				var snapshot = JsonSerializer.Deserialize<PausedTorrentSessionSnapshot>(json);

				if (snapshot == null)
				{
					_logger.Warn($"Snapshot deserialization failed for {torrentName}");
					return null;
				}

				var session = TorrentSession.CreateSessionFromSnapshot(snapshot);
				_logger.Info($"Resumed paused session for {torrentName}");
				return session;
			}
			catch (Exception ex)
			{
				_logger.Error($"Failed to load paused session for {torrentName}", ex);
				return null;
			}
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
			StartServiceTaskOnThreadPool(torrentSession, _trackerServiceTaskClient, cToken, "Tracker");
		}

		private void StartDownloadPhase(TorrentSession torrentSession, CancellationToken cToken)
		{
			StartServiceTaskOnThreadPool(torrentSession, _peerServiceTaskClient, cToken, "Peer");
			StartServiceTaskOnThreadPool(torrentSession, _storageServiceTaskClient, cToken, "Storage");
		}

		private void StartServiceTaskOnThreadPool(TorrentSession torrentSession, IServiceTaskClient serviceTaskClient, CancellationToken cToken, string serviceTaskName)
		{
			_ = Task.Run(async () =>
			{
				try
				{
					await serviceTaskClient.StartServiceTask(torrentSession, cToken);
				}
				catch (OperationCanceledException)
				{
					_logger.Info($"{serviceTaskName} service task canceled cleanly");
				}
				catch (Exception ex)
				{
					_logger.Error($"{serviceTaskName} service task failed", ex);
				}
			}, cToken);
			_logger.Info($"{serviceTaskName} service task started (Torrent Name: {torrentSession.Info.Name})");
		}

		private async Task HandleFileCompletion(TorrentSession torrentSession)
		{
			_logger.Info($"Finalising torrent download (Torrent Name: {torrentSession.Info.Name})");
			torrentSession.ShouldFlushOnShutdown = true;
			torrentSession.ShouldSnapshotOnShutdown = false;
			await torrentSession.Cts.CancelAsync();
			_torrentSessionManager.RemoveSession(torrentSession);

			var tcs = new TaskCompletionSource();
			torrentSession.OnPiecesFlushed.Subscribe(_ =>
			{
				tcs.TrySetResult();
			});
			await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(10)));

			var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(10)));
			if (completed == tcs.Task)
			{
				_logger.Info($"Pieces flushed successfully (Torrent Name: {torrentSession.Info.Name})");
				_logger.Info($"Successfully downloaded torrent file (Torrent Name: {torrentSession.Info.Name})");
			}
			else
			{
				_logger.Warn($"Timeout waiting for pieces to flush (Torrent Name: {torrentSession.Info.Name})");
			}
		}
	}
}
