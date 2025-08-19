using System.Net;
using System.Net.Sockets;
using System.Text;

namespace WiseTorrent.Peers.Types
{
	public class Peer : IDisposable
	{
		// Events
		public event Action<Peer, byte[]>? MessageReceived;
		public event Action<Peer>? PeerDisconnected;

		// Connection
		private TcpClient? _tcpClient;
		private NetworkStream? _stream;
		private readonly MemoryStream _receiveBuffer = new MemoryStream();

		// Identification
		public string PeerID { get; set; } // generate and stored in hashmap
		public required IPEndPoint IPEndPoint { get; set; }
		public byte[] PeerIDBytes => Encoding.UTF8.GetBytes(PeerID);

		public bool IsConnected { get; set; } = false;
		public DateTime LastActive { get; set; }

		// Protocol State
		public bool IsChoked { get; set; } = true;
		public bool IsInterested { get; set; } = false;
		public HashSet<int> AvailablePieces { get; set; } = new HashSet<int>();

		// Transfer Metrics
		public long BytesDownloaded { get; private set; }
		public long BytesUploaded { get; private set; }

		// Optimistic Unchoke
		public DateTime? LastOptimisticUnchokeTime { get; private set; }

		// Constructor
		public Peer() { }

		public Peer(TcpClient tcpClient)
		{
			_tcpClient = tcpClient;
			_stream = tcpClient.GetStream();

			StartListening();
		}

		// attempts to connect to 
		public void ConnectAsync()
		{
			throw new NotImplementedException();
		}
		private void StartListening()
		{
			Task.Run(() =>
			{
				try
				{
					byte[] buffer = new byte[4096];
					while (_tcpClient != null && _tcpClient.Connected)
					{
						int bytesRead = _stream!.Read(buffer, 0, buffer.Length);
						if (bytesRead > 0)
						{
							_receiveBuffer.Write(buffer, 0, bytesRead);
							BytesDownloaded += bytesRead;
							LastActive = DateTime.UtcNow;

							byte[] messageData = new byte[bytesRead];
							Array.Copy(buffer, messageData, bytesRead);
							MessageReceived?.Invoke(this, messageData);
						}
						else
						{
							break;
						}
					}
				}
				catch
				{
					// Connection error
				}

				Disconnect();
				PeerDisconnected?.Invoke(this);
			});
		}

		// Try send data to peer
		public bool TrySend(byte[] data)
		{
			if (_stream == null || !_stream.CanWrite) return false;

			try
			{
				_stream.Write(data, 0, data.Length);
				BytesUploaded += data.Length;
				LastActive = DateTime.UtcNow;
				return true;
			}
			catch
			{
				return false;
			}
		}

		// Read data from peer into buffer
		public int TryReceive()
		{
			if (_stream == null || !_stream.CanRead) return 0;

			byte[] buffer = new byte[4096];
			try
			{
				int bytesRead = _stream.Read(buffer, 0, buffer.Length);
				if (bytesRead > 0)
				{
					_receiveBuffer.Write(buffer, 0, bytesRead);
					BytesDownloaded += bytesRead;
					LastActive = DateTime.UtcNow;
				}
				return bytesRead;
			}
			catch
			{
				return 0;
			}
		}

		// Get buffered data (for message parsing)
		public byte[] GetBufferedData() => _receiveBuffer.ToArray();

		// Clear buffer after processing
		public void ClearBuffer() => _receiveBuffer.SetLength(0);

		// Performance scoring
		public double GetPerformanceScore() => BytesUploaded * 0.7 + BytesDownloaded * 0.3;

		public void MarkOptimisticallyUnchoked() => LastOptimisticUnchokeTime = DateTime.UtcNow;

		public bool WasRecentlyOptimisticallyUnchoked(int seconds) =>
			LastOptimisticUnchokeTime != null &&
			(DateTime.UtcNow - LastOptimisticUnchokeTime.Value).TotalSeconds < seconds;

		public override string ToString() =>
			IPEndPoint == null
				? "Unknown Peer"
				: $"{IPEndPoint.Address}:{IPEndPoint.Port} - ID: {PeerID ?? "Unknown"}";

		// Clean up resources
		public void Disconnect()
		{
			_stream?.Close();
			_tcpClient?.Close();
			_stream = null;
			_tcpClient = null;
		}

		public void Dispose()
		{
			Disconnect();
			_receiveBuffer.Dispose();
		}
	}
}
