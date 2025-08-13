using WiseTorrent.Parsing.Interfaces;
using WiseTorrent.Parsing.Types;
using WiseTorrent.Peers.Types;
using WiseTorrent.Trackers.Interfaces;
using WiseTorrent.Utilities.Interfaces;

namespace WiseTorrent.Trackers.Classes
{
	internal class HTTPTrackerClient : ITrackerClient
	{
		private readonly ITrackerResponseParser _responseParser;
		private readonly ILogger<HTTPTrackerClient> _logger;
		private const int DefaultIntervalSeconds = 1800;
		private const int FallbackIntervalSeconds = 30;

		private List<string> _trackerAddresses = new();
		private int _currentTrackerIndex = 0;
		private int _interval = 0;
		private CancellationTokenSource _cts = new();

		public event Action<List<Peer>>? OnTrackerResponse;

		public HTTPTrackerClient(ITrackerResponseParser responseParser, ILogger<HTTPTrackerClient> logger)
		{
			_responseParser = responseParser;
			_logger = logger;
		}

		public void InitialiseClient(List<string> trackerAddresses, Action<List<Peer>> onTrackerResponse)
		{
			_trackerAddresses = trackerAddresses;
			OnTrackerResponse = onTrackerResponse;
		}

		public async Task StartServiceTask()
		{
			while (!_cts.Token.IsCancellationRequested)
			{
				await RunServiceTask().ConfigureAwait(false);
				var delay = _interval > 0 ? _interval : DefaultIntervalSeconds;
				await Task.Delay(TimeSpan.FromSeconds(delay), _cts.Token).ConfigureAwait(false);
			}
		}

		private async Task RunServiceTask()
		{
			var trackerUrl = _trackerAddresses[_currentTrackerIndex];
			try
			{
				_logger.Info($"Announcing to tracker: {trackerUrl}");
				using var client = new HttpClient();
				using var request = new HttpRequestMessage(HttpMethod.Get, trackerUrl);
				var response = await client.SendAsync(request, _cts.Token).ConfigureAwait(false);
				response.EnsureSuccessStatusCode();
				var responseStr = await response.Content.ReadAsStringAsync(_cts.Token).ConfigureAwait(false);
				TrackerResponse? parsedResponse = _responseParser.ParseTrackerResponseFromString(responseStr);

				if (parsedResponse != null)
				{
					if (_interval != parsedResponse.Interval)
					{
						_logger.Info($"Tracker interval updated: {_interval} → {parsedResponse.Interval}");
						_interval = parsedResponse.Interval;
					}

					OnTrackerResponse?.Invoke(parsedResponse.Peers);
				}
				else
				{
					_logger.Warn("Tracker response was null");
					_interval = FallbackIntervalSeconds;
					RotateTracker();
				}

			}
			catch (Exception ex)
			{
				_logger.Error("HTTP GET request to tracker failed", ex);
				_interval = FallbackIntervalSeconds;
				RotateTracker();
			}
		}

		private void RotateTracker()
		{
			_currentTrackerIndex = (_currentTrackerIndex + 1) % _trackerAddresses.Count;
			_logger.Warn($"Switching to next tracker: {_trackerAddresses[_currentTrackerIndex]}");
		}

		public void StopServiceTask()
		{
			_cts.Cancel();
		}
	}
}
