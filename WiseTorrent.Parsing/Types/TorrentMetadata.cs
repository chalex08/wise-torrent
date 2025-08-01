using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WiseTorrent.Trackers;
using WiseTorrent.Trackers.Types;

namespace WiseTorrent.Parsing.Types
{
	public class TorrentMetadata
	{
		public PeerDiscoveryProtocol protocol;
		public string? announce;
		public TorrentInfo info;

		public TorrentMetadata(PeerDiscoveryProtocol protocol, string announce, TorrentInfo info)
		{
			this.protocol = protocol;
			this.announce = announce;
			this.info = info;
		}

		public TorrentMetadata(TorrentInfo info)
		{
			protocol = PeerDiscoveryProtocol.DHT;
			this.info = info;
		}
	}
}