using System.Net;
using System.Web;
using WiseTorrent.Parsing.Interfaces;
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

		public async Task<bool> RunServiceTask(TorrentSession torrentSession, CancellationToken cToken)
		{
			var shouldRotateTracker = false;
			try
			{
				using var client = new HttpClient();
				using var request = new HttpRequestMessage(HttpMethod.Get, BuildTrackerURL(torrentSession));

				_logger.Info("Sending announce request");
				var response = await client.SendAsync(request, cToken).ConfigureAwait(false);
				response.EnsureSuccessStatusCode();
				_logger.Info("Announce response successful");

				TrackerResponse? parsedResponse = await _responseParser.ParseHttpTrackerResponseAsync(response).ConfigureAwait(false);

				if (parsedResponse != null)
				{
					if (parsedResponse.FailureReason == null)
					{
						if (torrentSession.TrackerIntervalSeconds != parsedResponse.Interval)
						{
							_logger.Info($"Tracker interval updated: {torrentSession.TrackerIntervalSeconds} → {parsedResponse.Interval}");
							torrentSession.TrackerIntervalSeconds = (int)parsedResponse.Interval!;
						}

						_logger.Info("Peer list received, notifying listeners");
						var externalIp = await GetExternalIpAddressAsync(client);
						var newPeers = new ConcurrentSet<Peer>();
						newPeers.AddRange(parsedResponse.Peers!.Where(p => p.PeerID != torrentSession.LocalPeer.PeerID && !p.IPEndPoint.Address.Equals(externalIp)));
						torrentSession.OnTrackerResponse.NotifyListeners(newPeers);
						torrentSession.LeecherCount = (int)parsedResponse.Incomplete!;
						torrentSession.SeederCount = (int)parsedResponse.Complete!;
					}
					else
					{
						_logger.Warn("Tracker announce request failed: " + parsedResponse.FailureReason);
						torrentSession.TrackerIntervalSeconds = TrackerServiceTaskClient.FallbackIntervalSeconds;
						shouldRotateTracker = true;
					}
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

		private string BuildTrackerURL(TorrentSession torrentSession)
		{
			return torrentSession.CurrentTrackerUrl.Url
				+ "?info_hash=" + HttpUtility.UrlEncode(torrentSession.InfoHash)
				+ "&peer_id=" + HttpUtility.UrlEncode(torrentSession.LocalPeer.PeerIDBytes)
				+ "&port=" + torrentSession.LocalPeer.IPEndPoint.Port
				+ "&uploaded=" + torrentSession.Metrics.TotalUploadedBytes
				+ "&downloaded=" + torrentSession.Metrics.TotalUploadedBytes
				+ "&left=" + torrentSession.RemainingBytes
				+ (torrentSession.CurrentEvent != EventState.None ? "&event=" + torrentSession.CurrentEvent.ToURLString() : String.Empty);
		}

		private async Task<IPAddress?> GetExternalIpAddressAsync(HttpClient client)
		{
			try
			{
				var ipString = await client.GetStringAsync("https://api.ipify.org");
				ipString = ipString.Trim();

				return IPAddress.TryParse(ipString, out var ip) ? ip : null;
			}
			catch
			{
				return null;
			}
		}

	}
}
