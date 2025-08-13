using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WiseTorrent.Parsing.Interfaces;
using WiseTorrent.Parsing.Types;
using WiseTorrent.Peers.Types;

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
