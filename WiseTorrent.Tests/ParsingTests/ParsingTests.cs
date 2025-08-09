using WiseTorrent.Parsing.Classes;
using WiseTorrent.Parsing.Types;
using WiseTorrent.Trackers.Types;

namespace WiseTorrent.Tests.ParsingTests
{
	[TestFixture]
	public class ParsingTests()
	{
		private TorrentParser _parser;

		public void AssertEnumerableInstanceEquality<T>(Action<T, T> assertions, IEnumerable<T> actual, IEnumerable<T> expected)
		{
			for (var i = 0; i < actual.Count(); i++)
			{
				var passedAssertions = false;
				try
				{
					assertions(actual.ElementAt(i), expected.ElementAt(i));
					passedAssertions = true;
				}
				finally
				{
					Assert.True(passedAssertions);
				}
			}
		}

		[SetUp]
		public void SetUp()
		{
			_parser = new TorrentParser();
		}

		[Test]
		public void ParseTorrentFileFromPath_CorrectlyParsesFile()
		{
			string testingFolder = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Environment.CurrentDirectory))) ?? "";
			string filePath = testingFolder + "\\TestAssets\\big-buck-bunny.torrent";
			TorrentMetadata? parsedMetadata = _parser.ParseTorrentFileFromPath(filePath);

			Assert.IsNotNull(parsedMetadata);

			Assert.IsNotNull(parsedMetadata.Announce);
			Assert.That(parsedMetadata.Announce.Protocol, Is.EqualTo(PeerDiscoveryProtocol.UDP));
			Assert.That(parsedMetadata.Announce.Url, Is.EqualTo("udp://tracker.leechers-paradise.org:6969"));

			Assert.IsNotNull(parsedMetadata.AnnounceList);
			List<List<ServerURL>> expectedAnnounceList =
			[
				new () { new ServerURL("udp://tracker.leechers-paradise.org:6969") },
				new () { new ServerURL("udp://tracker.coppersurfer.tk:6969") },
				new () { new ServerURL("udp://tracker.opentrackr.org:1337") },
				new () { new ServerURL("udp://explodie.org:6969") },
				new () { new ServerURL("udp://tracker.empire-js.us:1337") },
				new () { new ServerURL("wss://tracker.btorrent.xyz") },
				new () { new ServerURL("wss://tracker.openwebtorrent.com") },
				new () { new ServerURL("wss://tracker.fastcast.nz") }
			];

			Action<ServerURL, ServerURL> announceListInnerAssertions = (actual, expected) =>
			{
				Assert.That(actual.Protocol, Is.EqualTo(expected.Protocol));
				Assert.That(actual.Url, Is.EqualTo(expected.Url));
			};
			Action<List<ServerURL>, List<ServerURL>> announceListAssertions = (actual, expected) =>
			{
				AssertEnumerableInstanceEquality(announceListInnerAssertions, actual, expected);
			};
			AssertEnumerableInstanceEquality(announceListAssertions, parsedMetadata.AnnounceList, expectedAnnounceList);

			Assert.IsNotNull(parsedMetadata.Comment);
			Assert.That(parsedMetadata.Comment, Is.EqualTo("WebTorrent <https://webtorrent.io>"));

			Assert.IsNotNull(parsedMetadata.CreatedBy);
			Assert.That(parsedMetadata.CreatedBy, Is.EqualTo("WebTorrent <https://webtorrent.io>"));

			Assert.IsNotNull(parsedMetadata.CreationDate);
			Assert.That(parsedMetadata.CreationDate, Is.EqualTo(DateTimeOffset.FromUnixTimeSeconds(1490916601).UtcDateTime));

			Assert.IsNotNull(parsedMetadata.Encoding);
			Assert.That(parsedMetadata.Encoding, Is.EqualTo("UTF-8"));

			Assert.IsNotNull(parsedMetadata.UrlList);
			AssertEnumerableInstanceEquality(announceListInnerAssertions, parsedMetadata.UrlList, new List<ServerURL> { new("https://webtorrent.io/torrents/") });

			Assert.IsNotNull(parsedMetadata.IsPrivate);
			Assert.That(parsedMetadata.IsPrivate, Is.False);

			Assert.IsNull(parsedMetadata.HttpSeeds);
			Assert.IsNull(parsedMetadata.Source);

			Assert.IsNotNull(parsedMetadata.Info);
			TorrentInfo info = parsedMetadata.Info;

			Assert.That(info.Name, Is.EqualTo("Big Buck Bunny"));

			Assert.True(info.PieceLength.Equals(new ByteSize(262144)));
			//Assert.That(info.PieceHashes, Is.EqualTo())
			Assert.IsNull(info.Length);
			Assert.That(info.IsMultiFile, Is.True);

			Assert.IsNotNull(info.Files);
			List<TorrentFile> expectedFiles = new()
			{
				new TorrentFile(new ByteSize(140), ["Big Buck Bunny.en.srt"]),
				new TorrentFile(new ByteSize(276134947), ["Big Buck Bunny.mp4"]),
				new TorrentFile(new ByteSize(310380), ["poster.jpg"])
			};

			Action<TorrentFile, TorrentFile> fileAssertions = (actual, expected) =>
			{
				Assert.True(actual.Length.Equals(expected.Length));
				Assert.That(actual.PathSegments, Is.EqualTo(expected.PathSegments));
				Assert.That(actual.RelativePath == expected.RelativePath);
			};
			AssertEnumerableInstanceEquality(fileAssertions, info.Files, expectedFiles);
		}
	}
}