using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace WiseTorrent.Utilities.Types
{
	public class ServerURL(string url)
	{
		public PeerDiscoveryProtocol Protocol { get; set; } = URLToProtocol(url);
		public Uri Url { get; set; } = new (url);

		private static PeerDiscoveryProtocol URLToProtocol(string url)
		{
			if (string.IsNullOrWhiteSpace(url)) return PeerDiscoveryProtocol.DHT;
			if (Regex.IsMatch(url, "^https", RegexOptions.IgnoreCase)) return PeerDiscoveryProtocol.HTTPS;
			if (Regex.IsMatch(url, "^http", RegexOptions.IgnoreCase)) return PeerDiscoveryProtocol.HTTP;
			if (Regex.IsMatch(url, "^udp", RegexOptions.IgnoreCase)) return PeerDiscoveryProtocol.UDP;
			if (Regex.IsMatch(url, "^ws", RegexOptions.IgnoreCase)) return PeerDiscoveryProtocol.WS;
			return PeerDiscoveryProtocol.INVALID;
		}

		public async Task<IPEndPoint> GetIPEndPoint()
		{
			var addresses = await Dns.GetHostAddressesAsync(Url.Host);
			var ip = addresses.First(a => a.AddressFamily == AddressFamily.InterNetwork);
			return new IPEndPoint(ip, Url.Port);
		}
	}
}
