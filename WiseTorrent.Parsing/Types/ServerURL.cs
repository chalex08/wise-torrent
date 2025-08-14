using System.Text.RegularExpressions;

namespace WiseTorrent.Parsing.Types
{
	public class ServerURL(string url)
	{
		public PeerDiscoveryProtocol Protocol { get; set; } = URLToProtocol(url);
		public string Url { get; set; } = url;

		private static PeerDiscoveryProtocol URLToProtocol(string url)
		{
			if (string.IsNullOrWhiteSpace(url)) return PeerDiscoveryProtocol.DHT;
			if (Regex.IsMatch(url, "^https", RegexOptions.IgnoreCase)) return PeerDiscoveryProtocol.HTTPS;
			if (Regex.IsMatch(url, "^http", RegexOptions.IgnoreCase)) return PeerDiscoveryProtocol.HTTP;
			if (Regex.IsMatch(url, "^udp", RegexOptions.IgnoreCase)) return PeerDiscoveryProtocol.UDP;
			if (Regex.IsMatch(url, "^ws", RegexOptions.IgnoreCase)) return PeerDiscoveryProtocol.WS;
			return PeerDiscoveryProtocol.INVALID;
		}
	}
}
