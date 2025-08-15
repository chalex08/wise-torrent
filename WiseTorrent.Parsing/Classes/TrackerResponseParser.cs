using WiseTorrent.Parsing.Interfaces;
using WiseTorrent.Parsing.Types;
using WiseTorrent.Utilities.Types;

namespace WiseTorrent.Parsing.Classes
{
	public class TrackerResponseParser : ITrackerResponseParser
	{
		public TrackerResponse? ParseTrackerResponseFromString(string rawResponse)
		{
			var peers = new List<Peer>();
			return new TrackerResponse(0, peers);
		}
	}
}
