using BencodeNET.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WiseTorrent.Parsing.Types;

namespace WiseTorrent.Parsing.Interfaces
{
	public interface ITorrentParser
	{
		TorrentMetadata? ParseTorrentFileFromPath(string path);
	}
}
