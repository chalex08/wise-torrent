using System.Net;
using System.Net.Sockets;
using System.Text;
using WiseTorrent.Parsing.Classes;
using WiseTorrent.Tests.UtilitiesTests;
using WiseTorrent.Trackers.Classes;
using WiseTorrent.Trackers.Interfaces;
using WiseTorrent.Utilities.Types;

namespace WiseTorrent.Tests.TrackersTests
{
	[TestFixture]
	public class TrackersTests
	{
		private HTTPTrackerClient _httpTrackerClient;
		private UDPTrackerClient _udpTrackerClient;
		private TorrentParser _parser;
		private TestLogger<TrackerServiceTaskClient> _logger;
		private Func<PeerDiscoveryProtocol, ITrackerClient> _trackerClientFactory;

		[SetUp]
		public void SetUp()
		{
			var bEncodeReader = new BEncodeReader(new TestLogger<BEncodeReader>());
			var trackerResponseParser = new TrackerResponseParser(new TestLogger<TrackerResponseParser>(), bEncodeReader);
			_httpTrackerClient = new HTTPTrackerClient(trackerResponseParser, new TestLogger<HTTPTrackerClient>());
			_udpTrackerClient = new UDPTrackerClient(new TestLogger<UDPTrackerClient>());

			_logger = new TestLogger<TrackerServiceTaskClient>();
			_trackerClientFactory = protocol =>
			{
				return protocol switch
				{
					PeerDiscoveryProtocol.HTTP or PeerDiscoveryProtocol.HTTPS => _httpTrackerClient,
					PeerDiscoveryProtocol.UDP => _udpTrackerClient,
					_ => throw new Exception()
				};
			};

			_parser = new TorrentParser(new TestLogger<TorrentParser>(), bEncodeReader);
		}

		private async Task AssertTrackerClientBehaviour(string fileName)
		{
			string testingFolder = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Environment.CurrentDirectory))) ?? "";
			string filePath = testingFolder + "\\TestAssets\\" + fileName;
			TorrentMetadata? parsedMetadata = _parser.ParseTorrentFileFromPath(filePath);
			if (parsedMetadata == null) return;
			var totalBytes = parsedMetadata.Info.IsMultiFile
				? parsedMetadata.Info.Files!.Select(f => f.Length.ConvertUnit(ByteUnit.Byte).Size).Sum()
				: parsedMetadata.Info.Length!.ConvertUnit(ByteUnit.Byte).Size;
			var host = Dns.GetHostEntry(Dns.GetHostName());
			var ip = host.AddressList.First(a => a.AddressFamily == AddressFamily.InterNetwork);
			var peerId = "-PREFIX-" + Guid.NewGuid().ToString("N").Substring(0, 12);

			var torrentSession = new TorrentSession
			{
				Info = parsedMetadata.Info,
				InfoHash = parsedMetadata.InfoHash,
				LocalPeer = new Peer { PeerID = peerId, IPEndPoint = new IPEndPoint(ip, 6881) },
				FileMap = new FileMap(0, new List<TorrentFile>()),
				TotalBytes = totalBytes,
				RemainingBytes = totalBytes,
				CurrentEvent = EventState.Started,
				TrackerUrls = parsedMetadata.AnnounceList?.SelectMany(urls => urls).ToList() ?? [parsedMetadata.Announce!],
				CurrentTrackerUrlIndex = 0,
				TrackerIntervalSeconds = 0
			};

			List<Peer> responsePeers = new();
			var trackerServiceTaskClient = new TrackerServiceTaskClient(_logger, _trackerClientFactory);

			var cts = new CancellationTokenSource();
			torrentSession.OnTrackerResponse.Subscribe(peers =>
			{
				responsePeers.AddRange(peers);
				cts.Cancel();
			});

			var serviceTaskResult = Task.Run(async () =>
			{
				try
				{
					await trackerServiceTaskClient.StartServiceTask(torrentSession, cts.Token);
				}
				catch (OperationCanceledException)
				{
					_logger.Info("Tracker service task canceled cleanly");
				}
				catch (Exception ex)
				{
					_logger.Error("Tracker service task failed", ex);
				}
			});

			await serviceTaskResult;
			Assert.IsNotEmpty(responsePeers, "At least one peer should be returned");
			Assert.IsTrue(responsePeers.All(p => p.IPEndPoint.Address != null), "All peers should have valid IP addresses");
			Assert.IsTrue(responsePeers.All(p => p.IPEndPoint.Port > 0 && p.IPEndPoint.Port <= 65535), "All peers should have valid ports");
			if (torrentSession.LeecherCount > 0 || torrentSession.SeederCount > 0)
				Assert.IsTrue(responsePeers.Count <= torrentSession.LeecherCount + torrentSession.SeederCount, "Leecher count + seeder count should be greater than or equal to total peer count");
		}

		[TestCase("ubuntu-22.04.5-desktop-amd64.iso.torrent")]
		[TestCase("debian-13.0.0-amd64-netinst.iso.torrent")]
		[TestCase("Adventures_of_Smiling_Jack_Ep7.avi.torrent")]
		public async Task HTTPTrackerClient_ReceivesPeerListFromTracker(string fileName)
		{
			await AssertTrackerClientBehaviour(fileName);
		}
	}
}
