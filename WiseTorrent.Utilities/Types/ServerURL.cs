using System.Net;
using System.Net.Sockets;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace WiseTorrent.Utilities.Types
{
	public class ServerURL
	{
		public PeerDiscoveryProtocol Protocol { get; set; }

		[JsonIgnore]
		public Uri Url { get; set; }

		[JsonPropertyName("Url")]
		public string UrlString
		{
			get => Url.ToString();
			set
			{
				Url = new Uri(value);
				Protocol = URLToProtocol(value);
			}
		}

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
		public ServerURL() { } // needed for deserialization
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

		public ServerURL(string url)
		{
			Url = new Uri(url);
			Protocol = URLToProtocol(url);
		}



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
