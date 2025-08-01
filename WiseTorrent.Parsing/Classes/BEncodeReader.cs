using BencodeNET.IO;
using BencodeNET.Objects;
using BencodeNET.Parsing;
using WiseTorrent.Parsing.Interfaces;

namespace WiseTorrent.Parsing.Classes
{
	internal class BEncodeReader : IBEncodeReader
	{
		public BDictionary? ParseTorrentFileFromPath(string path)
		{
			FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
			return ParseTorrentFileFromStream(stream);
		}

		BDictionary? ParseTorrentFileFromStream(Stream stream)
		{
			try
			{
				BencodeReader reader = new BencodeReader(stream);
				BencodeParser parser = new BencodeParser();
				BDictionary parsedObject = parser.Parse<BDictionary>(reader);
				return parsedObject;
			}
			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
				return null;
			}
		}

		BDictionary? IBEncodeReader.ParseTorrentFileFromStream(Stream stream)
		{
			return ParseTorrentFileFromStream(stream);
		}
	}
}