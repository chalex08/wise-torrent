using BencodeNET.Objects;
using WiseTorrent.Parsing.Interfaces;
using WiseTorrent.Utilities.Interfaces;
using WiseTorrent.Utilities.Types;

namespace WiseTorrent.Parsing.Classes
{
	internal class TrackerResponseParser : ITrackerResponseParser
	{
		private readonly ILogger<TrackerResponseParser> _logger;
		private readonly IBEncodeReader _bEncodeReader;

		public TrackerResponseParser(ILogger<TrackerResponseParser> logger, IBEncodeReader bEncodeReader)
		{
			_logger = logger;
			_bEncodeReader = bEncodeReader;
		}

		public async Task<TrackerResponse?> ParseHttpTrackerResponseAsync(HttpResponseMessage response)
		{
			_logger.Info("Parsing tracker response from string");
			BDictionary? decodedDict = await _bEncodeReader.ParseHttpTrackerResponseAsync(response);
			return decodedDict == null ? null : BuildTrackerResponse(decodedDict);
		}

		private TrackerResponse? BuildTrackerResponse(BDictionary decodedDict)
		{
			_logger.Info("Building tracker response from parsed response");
			return new TrackerResponseBuilder(decodedDict).Build();
		}

	}
}
