using BencodeNET.IO;
using BencodeNET.Objects;
using BencodeNET.Parsing;
using WiseTorrent.Parsing.Interfaces;
using WiseTorrent.Utilities.Interfaces;

namespace WiseTorrent.Parsing.Classes
{
	internal class BEncodeReader : IBEncodeReader
	{
		private readonly ILogger<BEncodeReader> _logger;

		public BEncodeReader(ILogger<BEncodeReader> logger)
		{
			_logger = logger;
		}

		public BDictionary? ParseTorrentFileFromPath(string path)
		{
			if (!File.Exists(path))
			{
				_logger.Error($"File not found: {path}");
				return null;
			}

			if (Path.GetExtension(path).ToLowerInvariant() != ".torrent")
			{
				_logger.Error($"Invalid file extension: {path}. Expected '.torrent'");
				return null;
			}

			try
			{
				using FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
				return ParseTorrentFileFromStream(stream);
			}
			catch (Exception e)
			{
				_logger.Error("Failed to open file", e);
				return null;
			}
		}

		private BDictionary? ParseTorrentFileFromStream(Stream stream)
		{
			try
			{
				BencodeReader reader = new BencodeReader(stream);
				BencodeParser parser = new BencodeParser();
				BDictionary parsedObject = parser.Parse<BDictionary>(reader);
				if (!parsedObject.ContainsKey("info"))
				{
					_logger.Error("Invalid torrent file: missing 'info' dictionary.");
					return null;
				}

				return parsedObject;
			}
			catch (Exception e)
			{
				_logger.Error("Parsing error", e);
				return null;
			}
		}

		public async Task<BDictionary?> ParseHttpTrackerResponseAsync(HttpResponseMessage response)
		{
			try
			{
				var bytes = await response.Content.ReadAsByteArrayAsync();
				var parser = new BencodeParser();
				return parser.Parse<BDictionary>(bytes);
			}
			catch
			{
				return null;
			}

		}

	}
}