using BencodeNET.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WiseTorrent.Parsing.Interfaces;
using WiseTorrent.Parsing.Types;
using WiseTorrent.Trackers;
using WiseTorrent.Trackers.Types;

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
			return decodedDict == null ? null : RawTorrentToFormatted(decodedDict);
		}

		internal TorrentMetadata? RawTorrentToFormatted(BDictionary decodedDict)
		{
			string trackerURL = "";
			if (decodedDict.TryGetValue("announce", out var announceValue) && announceValue != null)
			{
				trackerURL = announceValue.ToString() ?? "";
			}

			PeerDiscoveryProtocol protocol = TrackerURLToProtocol(trackerURL);
			BDictionary? infoDict = decodedDict["info"] as BDictionary;
			if (infoDict != null)
			{
				string torrentName = infoDict["name"].ToString() ?? "";
				ByteSize pieceLength = new ByteSize(((BNumber)infoDict["piece length"]).Value);
				byte[][] pieceHashes = ParsePieceHashes(((BString)infoDict["pieces"]).Value.ToArray());
				ByteSize fileLength = new ByteSize(((BNumber)infoDict["length"]).Value);
				TorrentInfo info = new TorrentInfo(torrentName, pieceLength, pieceHashes, fileLength);
				return new TorrentMetadata(protocol, trackerURL, info);
			}
			return null;
		}

		internal PeerDiscoveryProtocol TrackerURLToProtocol(string trackerURL)
		{
			if (trackerURL == "") return PeerDiscoveryProtocol.DHT;
			else if (Regex.IsMatch(trackerURL, "^http", RegexOptions.IgnoreCase))
			{
				return PeerDiscoveryProtocol.HTTP;
			}
			else if (Regex.IsMatch(trackerURL, "^udp", RegexOptions.IgnoreCase))
			{
				return PeerDiscoveryProtocol.UDP;
			}
			else
			{
				throw new Exception("Invalid peer discovery protocol");
			}
		}

		internal byte[][] ParsePieceHashes(byte[] hashStream)
		{
			int pieceCount;
			if ((pieceCount = hashStream.Length % 20) != 0) throw new Exception("Invalid pieces field: length must be a multiple of 20");

			byte[][] pieceHashes = new byte[pieceCount][];
			for (int i = 0; i < pieceCount; i++)
			{
				pieceHashes[i] = hashStream.Skip(i * 20).Take(20).ToArray();
			}

			return pieceHashes;
		}
	}
}
