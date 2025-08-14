using System.Net;
using System.Net.Sockets;
using WiseTorrent.Peers.Types;
using WiseTorrent.Trackers.Interfaces;
using WiseTorrent.Trackers.Types;
using WiseTorrent.Utilities.Interfaces;

namespace WiseTorrent.Trackers.Classes
{
	internal class UDPTrackerClient : ITrackerClient
	{
		private const long ProtocolId = 0x41727101980; // magic constant
		private const int ConnectAction = 0;
		private const int AnnounceAction = 1;

		private readonly ILogger<UDPTrackerClient> _logger;
		private readonly Random _random = new();

		private byte[] _infoHash;
		private Peer _userPeer;
		private EventState? _eventState;
		private int _uploadCount;
		private int _downloadCount;
		private long _remainingByteCount;

		public UDPTrackerClient(ILogger<UDPTrackerClient> logger)
		{
			_logger = logger;
		}

		public void InitialiseClientState(byte[] infoHash, Peer userPeer, EventState eventState, int uploadCount, int downloadCount, long remainingBytes)
		{
			_infoHash = infoHash;
			_userPeer = userPeer;
			_eventState = eventState;
			_uploadCount = uploadCount;
			_downloadCount = downloadCount;
			_remainingByteCount = remainingBytes;
		}

		public async Task<(int, bool)> RunServiceTask(int interval, string trackerAddress, Action<List<Peer>> onTrackerResponse, CancellationToken cToken)
		{
			var responseInterval = 0;
			var shouldRotateTracker = false;

			try
			{
				var uri = new Uri(trackerAddress);

				var host = uri.Host;
				var port = uri.Port;

				var addresses = await Dns.GetHostAddressesAsync(host, cToken);
				var ip = addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
				if (ip == null)
					return (TrackerServiceTaskClient.FallbackIntervalSeconds, true);

				var endpoint = new IPEndPoint(ip, port);
				using var udpClient = new UdpClient();

				udpClient.Client.SendTimeout = 5000;
				udpClient.Client.ReceiveTimeout = 5000;

				var transactionId = _random.Next();
				var connectRequest = BuildConnectRequest(transactionId);

				await udpClient.SendAsync(connectRequest, connectRequest.Length, endpoint);

				using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cToken);
				timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));

				var connectReceiveTask = udpClient.ReceiveAsync();
				var connectCompleted = await Task.WhenAny(connectReceiveTask, Task.Delay(Timeout.Infinite, timeoutCts.Token));

				if (connectCompleted != connectReceiveTask)
					throw new TimeoutException("Connect response timed out");

				var connectionId = ParseConnectResponse(connectReceiveTask.Result.Buffer, transactionId);

				var announceTransactionId = _random.Next();
				var announceRequest = BuildAnnounceRequest(connectionId, announceTransactionId);

				await udpClient.SendAsync(announceRequest, announceRequest.Length, endpoint);

				var announceReceiveTask = udpClient.ReceiveAsync();
				var announceCompleted = await Task.WhenAny(announceReceiveTask, Task.Delay(Timeout.Infinite, timeoutCts.Token));

				if (announceCompleted != announceReceiveTask)
					throw new TimeoutException("Announce response timed out");

				var (intervalSec, peers) = ParseAnnounceResponse(announceReceiveTask.Result.Buffer, announceTransactionId);

				responseInterval = intervalSec;
				onTrackerResponse(peers);
			}
			catch (Exception ex)
			{
				_logger.Error("UDP tracker communication failed", ex);
				responseInterval = TrackerServiceTaskClient.FallbackIntervalSeconds;
				shouldRotateTracker = true;
			}

			return (responseInterval, shouldRotateTracker);
		}

		private byte[] BuildConnectRequest(int transactionId)
		{
			var buffer = new byte[16];
			BitConverter.GetBytes(IPAddress.HostToNetworkOrder(ProtocolId)).CopyTo(buffer, 0);
			BitConverter.GetBytes(IPAddress.HostToNetworkOrder(ConnectAction)).CopyTo(buffer, 8);
			BitConverter.GetBytes(IPAddress.HostToNetworkOrder(transactionId)).CopyTo(buffer, 12);
			return buffer;
		}

		private long ParseConnectResponse(byte[] buffer, int expectedTransactionId)
		{
			var action = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, 0));
			var transactionId = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, 4));
			if (action != ConnectAction || transactionId != expectedTransactionId)
				throw new InvalidOperationException("Invalid connect response");

			return IPAddress.NetworkToHostOrder(BitConverter.ToInt64(buffer, 8));
		}

		private byte[] BuildAnnounceRequest(long connectionId, int transactionId)
		{
			var buffer = new byte[98];
			var offset = 0;

			void WriteInt(int value) => BitConverter.GetBytes(IPAddress.HostToNetworkOrder(value)).CopyTo(buffer, offset += 4 - 4);
			void WriteLong(long value) => BitConverter.GetBytes(IPAddress.HostToNetworkOrder(value)).CopyTo(buffer, offset += 8 - 8);
			void WriteBytes(byte[] data, int length) => data.CopyTo(buffer, offset += length - length);

			WriteLong(connectionId);
			WriteInt(AnnounceAction);
			WriteInt(transactionId);
			WriteBytes(_infoHash, 20);
			WriteBytes(_userPeer.PeerIDBytes, 20);
			WriteLong(_downloadCount);
			WriteLong(_remainingByteCount);
			WriteLong(_uploadCount);
			WriteInt((int)(_eventState ?? EventState.None));
			WriteInt(0); // IP address (0 for tracker to infer)
			WriteInt(_random.Next()); // key (random value for stats tracking)
			WriteInt(-1); // num_want (-1 for default)
			WriteInt(_userPeer.Port);

			return buffer;
		}

		private (int interval, List<Peer> peers) ParseAnnounceResponse(byte[] buffer, int expectedTransactionId)
		{
			var action = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, 0));
			var transactionId = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, 4));
			if (action != AnnounceAction || transactionId != expectedTransactionId)
				throw new InvalidOperationException("Invalid announce response");

			var interval = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, 8));
			var peerList = new List<Peer>();
			for (int i = 20; i < buffer.Length; i += 6)
			{
				var ip = new IPAddress(buffer[i..(i + 4)]);
				var port = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(buffer, i + 4));
				peerList.Add(new Peer{ IP = ip.ToString(), Port = port });
			}

			return (interval, peerList);
		}
	}
}