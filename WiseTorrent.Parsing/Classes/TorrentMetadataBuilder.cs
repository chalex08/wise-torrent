using System.Security.Cryptography;
using BencodeNET.Objects;
using WiseTorrent.Parsing.Types;

namespace WiseTorrent.Parsing.Builders
{
	internal class TorrentMetadataBuilder
	{
		private readonly BDictionary _rawDict;

		public TorrentMetadataBuilder(BDictionary rawDict)
		{
			_rawDict = rawDict;
		}

		public TorrentMetadata? Build()
		{
			if (!_rawDict.TryGetValue("info", out var infoObj) || infoObj is not BDictionary infoDict)
				return null;

			var hashBytes = SHA1.HashData(infoDict.EncodeAsBytes());
			var metadata = new TorrentMetadata
			{
				Announce = TryGetServerURL("announce"),
				AnnounceList = ParseTieredURLList("announce-list"),
				Comment = TryGetString(_rawDict, "comment"),
				CreatedBy = TryGetString(_rawDict, "created by"),
				CreationDate = ParseCreationDate(),
				Encoding = TryGetString(_rawDict, "encoding"),
				UrlList = ParseURLList("url-list"),
				HttpSeeds = ParseURLList("httpseeds"),
				Source = TryGetString(_rawDict, "source"),
				IsPrivate = infoDict.TryGetValue("private", out var priv) && priv.ToString() == "1",
				Info = ParseTorrentInfo(infoDict),
				InfoHash = hashBytes
			};

			return metadata;
		}

		private ServerURL TryGetServerURL(string key)
		{
			return new ServerURL(TryGetString(_rawDict, key) ?? "");
		}

		private string? TryGetString(BDictionary dict, string key)
		{
			return dict.TryGetValue(key, out var obj) ? obj.ToString() : null;
		}

		private ByteSize GetByteSize(BDictionary dict, string key)
		{
			return new ByteSize(((BNumber)dict[key]).Value);
		}

		private List<List<ServerURL>>? ParseTieredURLList(string key)
		{
			if (!_rawDict.TryGetValue(key, out var obj) || obj is not BList outer) return null;

			return outer
				.OfType<BList>()
				.Select(tier => tier
					.Select(url => new ServerURL(url.ToString() ?? ""))
					.Where(server => server.Protocol != PeerDiscoveryProtocol.INVALID)
					.ToList()
				)
				.Where(tier => tier.Count > 0)
				.ToList();
		}

		private List<ServerURL>? ParseURLList(string key)
		{
			if (!_rawDict.TryGetValue(key, out var obj) || obj is not BList outer) return null;

			return outer
				.Select(url => new ServerURL(url.ToString() ?? ""))
				.Where(server => server.Protocol != PeerDiscoveryProtocol.INVALID)
				.ToList();
		}

		private DateTime? ParseCreationDate()
		{
			if (_rawDict.TryGetValue("creation date", out var obj) && obj is BNumber num)
				return DateTimeOffset.FromUnixTimeSeconds(num.Value).UtcDateTime;
			return null;
		}

		private TorrentInfo ParseTorrentInfo(BDictionary infoDict)
		{
			string name = TryGetString(infoDict, "name") ?? "";
			ByteSize pieceLength = GetByteSize(infoDict, "piece length");
			byte[][] pieceHashes = ParsePieceHashes(((BString)infoDict["pieces"]).Value.ToArray());

			if (infoDict.TryGetValue("files", out var filesObj) && filesObj is BList fileList)
			{
				var files = new List<TorrentFile>();
				foreach (var fileObj in fileList.OfType<BDictionary>())
				{
					var length = GetByteSize(fileObj, "length");
					var path = ((BList)fileObj["path"]).Select(p => p.ToString() ?? "").ToList();
					files.Add(new TorrentFile(length, path));
				}
				return new TorrentInfo(name, pieceLength, pieceHashes, files);
			}

			return new TorrentInfo(name, pieceLength, pieceHashes, GetByteSize(infoDict, "length"));
		}

		private byte[][] ParsePieceHashes(byte[] hashStream)
		{
			if (hashStream.Length % 20 != 0)
				throw new Exception("Invalid pieces field: length must be a multiple of 20");

			int count = hashStream.Length / 20;
			var hashes = new byte[count][];
			for (int i = 0; i < count; i++)
				hashes[i] = hashStream.Skip(i * 20).Take(20).ToArray();

			return hashes;
		}
	}
}