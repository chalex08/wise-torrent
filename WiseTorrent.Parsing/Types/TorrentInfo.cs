using System;
using System.Collections.Generic;

namespace WiseTorrent.Parsing.Types
{
	public class TorrentInfo
	{
		public string Name { get; set; }
		public ByteSize PieceLength { get; set; }
		public byte[][] PieceHashes { get; set; }

		// single file torrent
		public ByteSize? Length { get; set; }

		// multi file torrent
		public List<TorrentFile>? Files { get; set; }
		public bool IsMultiFile => Files != null;

		public TorrentInfo(string name, ByteSize pieceLength, byte[][] pieceHashes, ByteSize length)
		{
			Name = name;
			PieceLength = pieceLength;
			PieceHashes = pieceHashes;
			Length = length;
		}

		public TorrentInfo(string name, ByteSize pieceLength, byte[][] pieceHashes, List<TorrentFile> files)
		{
			Name = name;
			PieceLength = pieceLength;
			PieceHashes = pieceHashes;
			Files = files;
		}
	}
}