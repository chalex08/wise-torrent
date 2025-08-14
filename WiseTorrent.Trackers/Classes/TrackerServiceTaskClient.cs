using WiseTorrent.Parsing.Types;
using WiseTorrent.Peers.Types;
using WiseTorrent.Trackers.Interfaces;
using WiseTorrent.Utilities.Interfaces;

namespace WiseTorrent.Trackers.Classes
{
	internal class TrackerServiceTaskClient : ITrackerServiceTaskClient
	{
		private readonly ILogger<TrackerServiceTaskClient> _logger;
		private readonly Func<PeerDiscoveryProtocol, ITrackerClient> _clients;
		private ITrackerClient _client;

		private List<ServerURL> _trackerAddresses = new();
		private int _currentTrackerIndex = 0;
		private ServerURL _currentTrackerUrl = new(String.Empty);
		private int _interval = 0;
		private readonly CancellationTokenSource _cts = new();

		private const int DefaultIntervalSeconds = 1800;
		public static readonly int FallbackIntervalSeconds = 30;

		private event Action<List<Peer>> OnTrackerResponse;

		public TrackerServiceTaskClient(ILogger<TrackerServiceTaskClient> logger, Func<PeerDiscoveryProtocol, ITrackerClient> clients)
		{
			_logger = logger;
			_clients = clients;
		}

		public void InitialiseClient(List<ServerURL> trackerAddresses, Action<List<Peer>> onTrackerResponse)
		{
			_trackerAddresses = trackerAddresses;
			OnTrackerResponse = onTrackerResponse;
			_currentTrackerIndex = 0;
			_currentTrackerUrl = _trackerAddresses[_currentTrackerIndex];
			SetClient(_currentTrackerUrl.Protocol);
		}

		public async Task StartServiceTask()
		{
			while (!_cts.Token.IsCancellationRequested)
			{
				var trackerUrl = _trackerAddresses[_currentTrackerIndex];

				(_interval, var shouldRotateTracker) = await _client.RunServiceTask(_interval, trackerUrl.Url, OnTrackerResponse, _cts.Token).ConfigureAwait(false);
				if (shouldRotateTracker) { RotateTracker(); }

				var delay = _interval > 0 ? _interval : DefaultIntervalSeconds;
				await Task.Delay(TimeSpan.FromSeconds(delay), _cts.Token).ConfigureAwait(false);
			}
		}

		private void SetClient(PeerDiscoveryProtocol protocol)
		{
			_client = _clients(protocol);
		}

		private void RotateTracker()
		{
			_currentTrackerIndex = (_currentTrackerIndex + 1) % _trackerAddresses.Count;
			_currentTrackerUrl = _trackerAddresses[_currentTrackerIndex];
			SetClient(_currentTrackerUrl.Protocol);
			_logger.Warn($"Switching to next tracker: {_currentTrackerUrl}");
		}

		public void StopServiceTask()
		{
			_cts.Cancel();
		}
	}
}
