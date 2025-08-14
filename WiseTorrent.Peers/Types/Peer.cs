using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace WiseTorrent.Peers.Types
{
    public class Peer : IDisposable
    {
        // Identification
        public string? PeerID { get; set; }
        public IPEndPoint? IPEndPoint { get; set; }

        // Connection
        private TcpClient? _tcpClient;
        private NetworkStream? _stream;
        private readonly MemoryStream _receiveBuffer = new MemoryStream();

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
        public Peer(TcpClient tcpClient, string? peerID = null)
        {
            _tcpClient = tcpClient;
            _stream = tcpClient.GetStream();
            PeerID = peerID;
            LastActive = DateTime.UtcNow;
        }

        // Send data to peer
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
        public byte[] GetBufferedData()
        {
            return _receiveBuffer.ToArray();
        }

        // Clear buffer after processing
        public void ClearBuffer()
        {
            _receiveBuffer.SetLength(0);
        }

        // Performance scoring
        public double GetPerformanceScore()
        {
            return BytesUploaded * 0.7 + BytesDownloaded * 0.3;
        }

        public void MarkOptimisticallyUnchoked()
        {
            LastOptimisticUnchokeTime = DateTime.UtcNow;
        }

        public bool WasRecentlyOptimisticallyUnchoked(int seconds)
        {
            return LastOptimisticUnchokeTime != null &&
                   (DateTime.UtcNow - LastOptimisticUnchokeTime.Value).TotalSeconds < seconds;
        }

        public override string ToString()
        {
            return IPEndPoint == null
                ? "Unknown Peer"
                : $"{IPEndPoint.Address}:{IPEndPoint.Port} - ID: {PeerID ?? "Unknown"}";
        }

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
