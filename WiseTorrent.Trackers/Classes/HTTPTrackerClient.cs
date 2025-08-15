using System.Web;
using WiseTorrent.Core.Types;
using WiseTorrent.Parsing.Interfaces;
using WiseTorrent.Parsing.Types;
using WiseTorrent.Trackers.Interfaces;
using WiseTorrent.Utilities.Interfaces;
using WiseTorrent.Utilities.Types;

namespace WiseTorrent.Trackers.Classes
{
	internal class HTTPTrackerClient : ITrackerClient
	{
		private readonly ILogger<HTTPTrackerClient> _logger;
		private readonly ITrackerResponseParser _responseParser;

		public HTTPTrackerClient(ITrackerResponseParser responseParser, ILogger<HTTPTrackerClient> logger)
		{
			_responseParser = responseParser;
			_logger = logger;
		}

		private string BuildTrackerURL(TorrentSession torrentSession)
		{
			return torrentSession.CurrentTrackerUrl.Url
				+ "?info_hash=" + HttpUtility.UrlEncode(torrentSession.InfoHash)
				+ "&peer_id=" + HttpUtility.UrlEncode(torrentSession.LocalPeer.PeerIDBytes)
				+ "&port=" + torrentSession.LocalPeer.IPEndPoint.Port
				+ "&uploaded=" + torrentSession.UploadedBytes
				+ "&downloaded=" + torrentSession.DownloadedBytes
				+ "&left=" + torrentSession.RemainingBytes
				+ (torrentSession.CurrentEvent != EventState.None ? "&event=" + torrentSession.CurrentEvent.ToURLString() : String.Empty);
		}

		public async Task<bool> RunServiceTask(TorrentSession torrentSession, CancellationToken cToken)
		{
			var shouldRotateTracker = false;
			try
			{

				_logger.Info($"Announcing to tracker: {torrentSession.CurrentTrackerUrl.Url}");
				using var client = new HttpClient();
				using var request = new HttpRequestMessage(HttpMethod.Get, BuildTrackerURL(torrentSession));
				var response = await client.SendAsync(request, cToken).ConfigureAwait(false);
				response.EnsureSuccessStatusCode();
				var responseStr = await response.Content.ReadAsStringAsync(cToken).ConfigureAwait(false);
				TrackerResponse? parsedResponse = _responseParser.ParseTrackerResponseFromString(responseStr);

				if (parsedResponse != null)
				{
					if (torrentSession.TrackerIntervalSeconds != parsedResponse.Interval)
					{
						_logger.Info($"Tracker interval updated: {torrentSession.TrackerIntervalSeconds} → {parsedResponse.Interval}");
						torrentSession.TrackerIntervalSeconds = parsedResponse.Interval;
					}

					torrentSession.OnTrackerResponse.NotifyListeners(parsedResponse.Peers);
				}
				else
				{
					_logger.Warn("Tracker response was null");
					torrentSession.TrackerIntervalSeconds = TrackerServiceTaskClient.FallbackIntervalSeconds;
					shouldRotateTracker = true;
				}

			}
			catch (Exception ex)
			{
				_logger.Error("HTTP GET request to tracker failed", ex);
				torrentSession.TrackerIntervalSeconds = TrackerServiceTaskClient.FallbackIntervalSeconds;
				shouldRotateTracker = true;
			}

			return shouldRotateTracker;
		}
	}
}
