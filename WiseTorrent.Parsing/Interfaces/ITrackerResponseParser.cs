using WiseTorrent.Parsing.Types;

namespace WiseTorrent.Parsing.Interfaces
{
	public interface ITrackerResponseParser
	{
		TrackerResponse? ParseTrackerResponseFromString(string rawResponse);
	}
}
