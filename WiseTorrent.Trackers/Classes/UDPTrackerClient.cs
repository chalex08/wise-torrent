using System.Net;
using System.Net.Sockets;
using WiseTorrent.Trackers.Interfaces;
using WiseTorrent.Utilities.Interfaces;
using WiseTorrent.Utilities.Types;

namespace WiseTorrent.Trackers.Classes
{
	internal class UDPTrackerClient : ITrackerClient
	{
		private const long ProtocolId = 0x41727101980; // magic constant
		private const int ConnectAction = 0;
		private const int AnnounceAction = 1;
		private const int UdpClientActionTimeoutSeconds = 5;

		private readonly ILogger<UDPTrackerClient> _logger;
		private readonly Random _random = new();

		public UDPTrackerClient(ILogger<UDPTrackerClient> logger)
		{
			_logger = logger;
		}

		public async Task<bool> RunServiceTask(TorrentSession torrentSession, CancellationToken cToken)
		{
			var shouldRotateTracker = false;
			try
			{
				using var udpClient = new UdpClient(torrentSession.LocalPeer.IPEndPoint);
				udpClient.Client.SendTimeout = UdpClientActionTimeoutSeconds * 1000;
				udpClient.Client.ReceiveTimeout = UdpClientActionTimeoutSeconds * 1000;

				using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cToken);
				timeoutCts.CancelAfter(TimeSpan.FromSeconds(UdpClientActionTimeoutSeconds));

				var endpoint = await torrentSession.CurrentTrackerUrl.GetIPEndPoint();

				_logger.Info("Performing tracker handshake");
				await PerformTrackerHandshake(udpClient, endpoint, torrentSession, timeoutCts.Token);

				_logger.Info("Performing tracker announce");
				var peers = await PerformTrackerAnnounce(udpClient, endpoint, torrentSession, timeoutCts.Token);

				_logger.Info("Peer list received, notifying listeners");
				torrentSession.OnTrackerResponse.NotifyListeners(peers);
			}
			catch (Exception ex)
			{
				_logger.Error("UDP tracker communication failed", ex);
				torrentSession.TrackerIntervalSeconds = TrackerServiceTaskClient.FallbackIntervalSeconds;
				shouldRotateTracker = true;
			}

			return shouldRotateTracker;
		}

		private async Task PerformTrackerHandshake(UdpClient udpClient, IPEndPoint endpoint, TorrentSession torrentSession, CancellationToken timeoutCt)
		{
			var transactionId = _random.Next();
			var connectRequest = BuildConnectRequest(transactionId);

			_logger.Info("Sending connect request");
			await udpClient.SendAsync(connectRequest, connectRequest.Length, endpoint);

			_logger.Info($"Connect request sent. Waiting {UdpClientActionTimeoutSeconds} seconds for response");
			var connectReceiveTask = udpClient.ReceiveAsync();
			var connectCompleted = await Task.WhenAny(connectReceiveTask, Task.Delay(Timeout.Infinite, timeoutCt));

			if (connectCompleted != connectReceiveTask)
				throw new TimeoutException("Connect response timed out");

			_logger.Info("Connection response received successfully");
			torrentSession.ConnectionId = ParseConnectResponseForConnectionID(connectReceiveTask.Result.Buffer, transactionId);
			_logger.Info($"Connection ID received successfully: {torrentSession.ConnectionId}");
		}

		private byte[] BuildConnectRequest(int transactionId)
		{
			var buffer = new byte[16];
			BitConverter.GetBytes(IPAddress.HostToNetworkOrder(ProtocolId)).CopyTo(buffer, 0);
			BitConverter.GetBytes(IPAddress.HostToNetworkOrder(ConnectAction)).CopyTo(buffer, 8);
			BitConverter.GetBytes(IPAddress.HostToNetworkOrder(transactionId)).CopyTo(buffer, 12);
			return buffer;
		}

		private long ParseConnectResponseForConnectionID(byte[] buffer, int expectedTransactionId)
		{
			var action = NetworkToHostInt32(buffer, 0);
			var transactionId = NetworkToHostInt32(buffer, 4);
			if (action != ConnectAction || transactionId != expectedTransactionId)
				throw new InvalidOperationException("Invalid connect response");

			return IPAddress.NetworkToHostOrder(BitConverter.ToInt64(buffer, 8));
		}

		private async Task<List<Peer>> PerformTrackerAnnounce(UdpClient udpClient, IPEndPoint endpoint, TorrentSession torrentSession, CancellationToken timeoutCt)
		{
			var announceTransactionId = _random.Next();
			var announceRequest = BuildAnnounceRequest(announceTransactionId, torrentSession);
			_logger.Info($"Announce packet length: {announceRequest.Length}");

			_logger.Info("Sending announce request");
			await udpClient.SendAsync(announceRequest, announceRequest.Length, endpoint);

			_logger.Info($"Announce request sent. Waiting {UdpClientActionTimeoutSeconds} seconds for response");
			var announceReceiveTask = udpClient.ReceiveAsync();
			var announceCompleted = await Task.WhenAny(announceReceiveTask, Task.Delay(Timeout.Infinite, timeoutCt));

			if (announceCompleted != announceReceiveTask)
				throw new TimeoutException("Announce response timed out");

			_logger.Info("Announce response received successfully");
			return ParseAnnounceResponse(announceReceiveTask.Result.Buffer, announceTransactionId, torrentSession);
		}

		private byte[] BuildAnnounceRequest(int transactionId, TorrentSession torrentSession)
		{
			var buffer = new byte[98];
			var offset = 0;

			void WriteInt(int value) => BitConverter.GetBytes(IPAddress.HostToNetworkOrder(value)).CopyTo(buffer, offset += 4 - 4);
			void WriteLong(long value) => BitConverter.GetBytes(IPAddress.HostToNetworkOrder(value)).CopyTo(buffer, offset += 8 - 8);
			void WriteBytes(byte[] data, int length) => data.CopyTo(buffer, offset += length - length);

			WriteLong(torrentSession.ConnectionId);
			WriteInt(AnnounceAction);
			WriteInt(transactionId);
			WriteBytes(torrentSession.InfoHash, 20);
			WriteBytes(torrentSession.LocalPeer.PeerIDBytes, 20);
			WriteLong(torrentSession.Metrics.TotalDownloadedBytes);
			WriteLong(torrentSession.RemainingBytes);
			WriteLong(torrentSession.Metrics.TotalUploadedBytes);
			WriteInt((int)torrentSession.CurrentEvent);
			WriteInt(0); // IP address (0 for tracker to infer)
			WriteInt(_random.Next()); // key (random value for stats tracking)
			WriteInt(-1); // num_want (-1 for default)
			WriteInt(torrentSession.LocalPeer.IPEndPoint.Port);

			return buffer;
		}

		private List<Peer> ParseAnnounceResponse(byte[] buffer, int expectedTransactionId, TorrentSession torrentSession)
		{
			var action = NetworkToHostInt32(buffer, 0);
			var transactionId = NetworkToHostInt32(buffer, 4);
			if (action != AnnounceAction || transactionId != expectedTransactionId)
				throw new InvalidOperationException("Invalid announce response");

			torrentSession.TrackerIntervalSeconds = NetworkToHostInt32(buffer, 8);
			torrentSession.LeecherCount = NetworkToHostInt32(buffer, 12);
			torrentSession.SeederCount = NetworkToHostInt32(buffer, 16);

			var peerList = new List<Peer>();
			for (int i = 20; i < buffer.Length; i += 6)
			{
				var ip = new IPAddress(buffer[i..(i + 4)]);
				var port = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(buffer, i + 4));
				peerList.Add(new Peer{ IPEndPoint = new IPEndPoint(ip, port)});
			}

			return peerList;
		}

		private int NetworkToHostInt32(byte[] buffer, int startByte)
		{
			return IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, startByte));
		}
	}
}