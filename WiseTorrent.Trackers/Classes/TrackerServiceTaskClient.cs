using WiseTorrent.Trackers.Interfaces;
using WiseTorrent.Utilities.Interfaces;
using WiseTorrent.Utilities.Types;

namespace WiseTorrent.Trackers.Classes
{
	internal class TrackerServiceTaskClient : ITrackerServiceTaskClient
	{
		private readonly ILogger<TrackerServiceTaskClient> _logger;
		private readonly Func<PeerDiscoveryProtocol, ITrackerClient> _clients;
		private ITrackerClient _client;
		private CancellationToken CToken { get; set; }
		private readonly TorrentSession _torrentSession;

		private const int DefaultIntervalSeconds = 1800;
		public static readonly int FallbackIntervalSeconds = 30;

		public TrackerServiceTaskClient(ILogger<TrackerServiceTaskClient> logger, Func<PeerDiscoveryProtocol, ITrackerClient> clients, TorrentSession torrentSession)
		{
			_logger = logger;
			_clients = clients;
			_torrentSession = torrentSession;
			_client = _clients(torrentSession.CurrentTrackerUrl.Protocol);
		}

		public async Task StartServiceTask(CancellationToken cToken)
		{
			CToken = cToken;
			_logger.Info("Tracker service task started");
			while (!CToken.IsCancellationRequested)
			{
				_logger.Info($"Running tracker service task on {_torrentSession.CurrentTrackerUrl.Url}, using {_torrentSession.CurrentTrackerUrl.Protocol}");
				var shouldRotateTracker = await _client.RunServiceTask(_torrentSession, CToken).ConfigureAwait(false);
				if (shouldRotateTracker) { RotateTracker(); }

				var delaySeconds = _torrentSession.TrackerIntervalSeconds > 0 ? _torrentSession.TrackerIntervalSeconds : DefaultIntervalSeconds;
				var delayMinutes = delaySeconds / 60;
				var delaySecondsRemainder = delaySeconds % 60;
				var timeString = delayMinutes > 0
					? $"{delayMinutes} minute{(delayMinutes == 1 ? "" : "s")}" + (delaySecondsRemainder > 0 ? $" {delaySecondsRemainder} second{(delaySecondsRemainder == 1 ? "" : "s")}" : "")
					: $"{delaySecondsRemainder} second{(delaySecondsRemainder == 1 ? "" : "s")}";

				_logger.Info($"Tracker service task will next run in {timeString}");
				await Task.Delay(TimeSpan.FromSeconds(delaySeconds), CToken).ConfigureAwait(false);
			}

			_logger.Info("Tracker service task stopped");
		}

		private void RotateTracker()
		{
			_torrentSession.CurrentTrackerUrlIndex = (_torrentSession.CurrentTrackerUrlIndex + 1) % _torrentSession.TrackerUrls.Count;
			_client = _clients(_torrentSession.CurrentTrackerUrl.Protocol);
			_logger.Warn($"Switching to next tracker: {_torrentSession.CurrentTrackerUrl.Url}");
		}
	}
}
