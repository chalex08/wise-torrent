using BencodeNET.IO;
using BencodeNET.Objects;
using BencodeNET.Parsing;
using WiseTorrent.Parsing.Interfaces;

namespace WiseTorrent.Parsing.Classes
{
	internal static class BEncodeReader
	{
		public static BDictionary? ParseTorrentFileFromPath(string path)
		{
			if (!File.Exists(path))
			{
				Console.WriteLine($"File not found: {path}");
				return null;
			}

			if (Path.GetExtension(path).ToLowerInvariant() != ".torrent")
			{
				Console.WriteLine($"Invalid file extension: {path}. Expected '.torrent'");
				return null;
			}

			try
			{
				using FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
				return ParseTorrentFileFromStream(stream);
			}
			catch (Exception e)
			{
				Console.WriteLine($"Failed to open file: {e}");
				return null;
			}
		}

		private static BDictionary? ParseTorrentFileFromStream(Stream stream)
		{
			try
			{
				BencodeReader reader = new BencodeReader(stream);
				BencodeParser parser = new BencodeParser();
				BDictionary parsedObject = parser.Parse<BDictionary>(reader);
				if (!parsedObject.ContainsKey("info"))
				{
					Console.WriteLine("Invalid torrent file: missing 'info' dictionary.");
					return null;
				}

				return parsedObject;
			}
			catch (Exception e)
			{
				Console.WriteLine($"Parsing error: {e}");
				return null;
			}
		}
	}
}