using System.Text;
using WiseTorrent.Utilities.Types;

namespace WiseTorrent.Parsing.Interfaces
{
	public interface ITrackerResponseParser
	{
		Task<TrackerResponse?> ParseHttpTrackerResponseAsync(HttpResponseMessage response);
	}
}
