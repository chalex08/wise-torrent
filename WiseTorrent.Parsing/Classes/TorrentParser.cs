using BencodeNET.Objects;
using WiseTorrent.Parsing.Builders;
using WiseTorrent.Parsing.Interfaces;
using WiseTorrent.Utilities.Types;

namespace WiseTorrent.Parsing.Classes
{
	internal class TorrentParser : ITorrentParser
	{
		public TorrentMetadata? ParseTorrentFileFromPath(string path)
		{
			BDictionary? decodedDict = BEncodeReader.ParseTorrentFileFromPath(path);
			return decodedDict == null ? null : BuildTorrentMetadata(decodedDict);
		}

		private TorrentMetadata? BuildTorrentMetadata(BDictionary decodedDict)
		{
			return new TorrentMetadataBuilder(decodedDict).Build();
		}
	}
}
