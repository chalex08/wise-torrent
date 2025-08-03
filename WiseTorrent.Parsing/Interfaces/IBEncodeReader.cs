using BencodeNET.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiseTorrent.Parsing.Interfaces
{
	public interface IBEncodeReader
	{
		BDictionary? ParseTorrentFileFromPath(string path);
	}
}
