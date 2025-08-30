using WiseTorrent.Trackers.Interfaces;
using WiseTorrent.Utilities.Interfaces;
using WiseTorrent.Utilities.Types;

namespace WiseTorrent.Trackers.Classes
{
	internal class TrackerServiceTaskClient : ITrackerServiceTaskClient
	{
		private readonly ILogger<TrackerServiceTaskClient> _logger;
		private readonly Func<PeerDiscoveryProtocol, ITrackerClient> _clients;
		private ITrackerClient? Client { get; set; }
		private CancellationToken CToken { get; set; }

		private const int DefaultIntervalSeconds = 1800;
		public static readonly int FallbackIntervalSeconds = 5;

		public TrackerServiceTaskClient(ILogger<TrackerServiceTaskClient> logger, Func<PeerDiscoveryProtocol, ITrackerClient> clients)
		{
			_logger = logger;
			_clients = clients;
		}

		public async Task StartServiceTask(TorrentSession torrentSession, CancellationToken cToken)
		{
			CToken = cToken;
			Client = _clients(torrentSession.CurrentTrackerUrl.Protocol);

			_logger.Info("Tracker service task started");
			while (!CToken.IsCancellationRequested)
			{
				_logger.Info($"Running tracker service task on {torrentSession.CurrentTrackerUrl.Url}, using {torrentSession.CurrentTrackerUrl.Protocol}");
				var shouldRotateTracker = await Client.RunServiceTask(torrentSession, CToken).ConfigureAwait(false);
				if (shouldRotateTracker) { RotateTracker(torrentSession); }

				var delaySeconds = torrentSession.TrackerIntervalSeconds > 0 ? torrentSession.TrackerIntervalSeconds : DefaultIntervalSeconds;
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

		private void RotateTracker(TorrentSession torrentSession)
		{
			torrentSession.CurrentTrackerUrlIndex = (torrentSession.CurrentTrackerUrlIndex + 1) % torrentSession.TrackerUrls.Count;
			Client = _clients(torrentSession.CurrentTrackerUrl.Protocol);
			_logger.Warn($"Switching to next tracker: {torrentSession.CurrentTrackerUrl.Url}");
		}
	}
}
