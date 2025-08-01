using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiseTorrent.Parsing.Types
{
	public class TorrentInfo
	{
		public string name;
		public ByteSize pieceLength;
		public byte[][] pieceHashes;
		public ByteSize? length;
		public (ByteSize, string)[]? files;

		public TorrentInfo(string name, ByteSize pieceLength, byte[][] pieceHashes, ByteSize length)
		{
			this.name = name;
			this.pieceLength = pieceLength;
			this.pieceHashes = pieceHashes;
			this.length = length;
		}

		public TorrentInfo(string name, ByteSize pieceLength, byte[][] pieceHashes, (ByteSize, string)[] files)
		{
			this.name = name;
			this.pieceLength = pieceLength;
			this.pieceHashes = pieceHashes;
			this.files = files;
		}
	}
}
