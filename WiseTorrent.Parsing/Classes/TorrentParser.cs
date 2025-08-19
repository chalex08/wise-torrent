using BencodeNET.Objects;
using WiseTorrent.Parsing.Interfaces;
using WiseTorrent.Utilities.Interfaces;
using WiseTorrent.Utilities.Types;

namespace WiseTorrent.Parsing.Classes
{
	internal class TorrentParser : ITorrentParser
	{
		private readonly ILogger<TorrentParser> _logger;
		private readonly IBEncodeReader _bEncodeReader;

		public TorrentParser(ILogger<TorrentParser> logger, IBEncodeReader bEncodeReader)
		{
			_logger = logger;
			_bEncodeReader = bEncodeReader;
		}

		public TorrentMetadata? ParseTorrentFileFromPath(string path)
		{
			_logger.Info("Parsing torrent file from path");
			BDictionary? decodedDict = _bEncodeReader.ParseTorrentFileFromPath(path);
			return decodedDict == null ? null : BuildTorrentMetadata(decodedDict);
		}

		private TorrentMetadata? BuildTorrentMetadata(BDictionary decodedDict)
		{
			_logger.Info("Building torrent metadata from parsed torrent file");
			return new TorrentMetadataBuilder(decodedDict).Build();
		}
	}
}
