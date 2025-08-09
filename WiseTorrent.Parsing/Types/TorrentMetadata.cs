using System;
using System.Collections.Generic;
using WiseTorrent.Trackers;
using WiseTorrent.Trackers.Types;

namespace WiseTorrent.Parsing.Types
{
	public class TorrentMetadata
	{
		public ServerURL? Announce { get; set; }
		public List<List<ServerURL>>? AnnounceList { get; set; }
		public string? Comment { get; set; }
		public string? CreatedBy { get; set; }
		public DateTime? CreationDate { get; set; }
		public string? Encoding { get; set; }
		public List<ServerURL>? UrlList { get; set; }
		public List<ServerURL>? HttpSeeds { get; set; }
		public bool? IsPrivate { get; set; }
		public string? Source { get; set; }

		public required TorrentInfo Info { get; set; }
	}
}