using BencodeNET.Objects;
using WiseTorrent.Parsing.Builders;
using WiseTorrent.Parsing.Interfaces;
using WiseTorrent.Parsing.Types;

namespace WiseTorrent.Parsing.Classes
{
	internal class TorrentParser : ITorrentParser
	{
		private readonly IBEncodeReader _bEncodeReader;

		public TorrentParser(IBEncodeReader bEncodeReader)
		{
			_bEncodeReader = bEncodeReader;
		}

		public TorrentMetadata? ParseTorrentFileFromPath(string path)
		{
			BDictionary? decodedDict = _bEncodeReader.ParseTorrentFileFromPath(path);
			return decodedDict == null ? null : BuildTorrentMetadata(decodedDict);
		}

		private TorrentMetadata? BuildTorrentMetadata(BDictionary decodedDict)
		{
			return new TorrentMetadataBuilder(decodedDict).Build();
		}
	}
}
