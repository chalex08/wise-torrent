using BencodeNET.Objects;
using System.Net;
using WiseTorrent.Utilities.Types;

namespace WiseTorrent.Parsing.Classes
{
	internal class TrackerResponseBuilder
	{
		private readonly BDictionary _rawDict;

		public TrackerResponseBuilder(BDictionary rawDict)
		{
			_rawDict = rawDict;
		}

		public TrackerResponse? Build()
		{
			if (_rawDict.TryGetValue("failure reason", out var fail))
			{
				return new TrackerResponse
				{
					FailureReason = fail.ToString()
				};
			}

			var peers = ParseNonCompactPeers();
			if (peers == null)
				return null;

			return new TrackerResponse
			{
				Interval = TryGetInt("interval"),
				Complete = TryGetInt("complete"),
				Incomplete = TryGetInt("incomplete"),
				WarningMessage = TryGetString("warning message"),
				Peers = peers
			};
		}

		private int TryGetInt(string key)
		{
			return _rawDict.TryGetValue(key, out var obj) && obj is BNumber num ? (int)num.Value : 0;
		}

		private string? TryGetString(string key)
		{
			return _rawDict.TryGetValue(key, out var obj) ? obj.ToString() : null;
		}

		private List<Peer>? ParseNonCompactPeers()
		{
			if (!_rawDict.TryGetValue("peers", out var obj) || obj is not BList peerList)
				return null;

			var peers = new List<Peer>(peerList.Count);
			foreach (var item in peerList)
			{
				if (item is not BDictionary peerDict)
					continue;

				if (!peerDict.TryGetValue("ip", out var ipObj) || ipObj is not BString ipStr)
					continue;

				if (!IPAddress.TryParse(ipStr.ToString(), out var ip))
					continue;

				if (!peerDict.TryGetValue("port", out var portObj) || portObj is not BNumber portNum)
					continue;

				int port = (int)portNum.Value;

				string? peerId = null;
				if (peerDict.TryGetValue("peer id", out var peerIdObj))
					peerId = peerIdObj.ToString();

				peers.Add(new Peer
				{
					IPEndPoint = new IPEndPoint(ip, port),
					PeerID = peerId
				});
			}

			return peers;
		}
	}
}
