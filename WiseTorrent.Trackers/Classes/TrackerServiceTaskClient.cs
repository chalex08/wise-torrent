using WiseTorrent.Core.Types;
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
		private readonly CancellationTokenSource _cts = new();
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

		public async Task StartServiceTask()
		{
			while (!_cts.Token.IsCancellationRequested)
			{
				var shouldRotateTracker = await _client.RunServiceTask(_torrentSession, _cts.Token).ConfigureAwait(false);
				if (shouldRotateTracker) { RotateTracker(); }

				var delay = _torrentSession.TrackerIntervalSeconds > 0 ? _torrentSession.TrackerIntervalSeconds : DefaultIntervalSeconds;
				await Task.Delay(TimeSpan.FromSeconds(delay), _cts.Token).ConfigureAwait(false);
			}
		}

		private void RotateTracker()
		{
			_torrentSession.CurrentTrackerUrlIndex = (_torrentSession.CurrentTrackerUrlIndex + 1) % _torrentSession.TrackerUrls.Count;
			_client = _clients(_torrentSession.CurrentTrackerUrl.Protocol);
			_logger.Warn($"Switching to next tracker: {_torrentSession.CurrentTrackerUrl.Url}");
		}

		public void StopServiceTask()
		{
			_cts.Cancel();
		}
	}
}
