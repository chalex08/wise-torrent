using WiseTorrent.Peers.Classes;
using WiseTorrent.Pieces.Interfaces;
using WiseTorrent.Utilities.Interfaces;
using WiseTorrent.Utilities.Types;

namespace WiseTorrent.Peers.Interfaces
{
	public interface IPeerManager
	{
		IPieceManager? PieceManager { get; set; }
		Task HandleTrackerResponse(TorrentSession torrentSession, ILogger<PeerConnector> peerConnectorsLogger, CancellationToken cToken, List<Peer>? newPeers = null);
		void TryQueueMessage(Peer peer, PeerMessage peerMessage);
		Task<bool> SendPeerMessageAsync(Peer peer, byte[] data, CancellationToken cToken);
		Task<byte[]> ReceivePeerMessageAsync(Peer peer, CancellationToken cToken);
		void QueuePieceRequests(Peer peer, CancellationToken token);
		int GetPieceLength(int index);
		Task DisconnectAllPeersAsync(CancellationToken cToken);
		Task DisconnectPeerAsync(Peer peer, CancellationToken cToken);
		void UpdatePeerStatesAsync(CancellationToken cToken);
		Task UpdatePeerSelectionAsync(CancellationToken cToken);
	}
}
