using WiseTorrent.Peers.Classes;
using WiseTorrent.Utilities.Interfaces;
using WiseTorrent.Utilities.Types;

namespace WiseTorrent.Peers.Interfaces
{
	public interface IPeerManager
	{
		Task HandleTrackerResponse(TorrentSession torrentSession, ILogger<PeerConnector> peerConnectorsLogger, CancellationToken cToken);
		Task ConnectToAllPeersAsync(CancellationToken cToken);
		Task ConnectToPeerAsync(Peer peer, CancellationToken cToken);
		void TryQueueMessage(Peer peer, PeerMessage peerMessage);
		Task<bool> SendPeerMessageAsync(Peer peer, byte[] data, CancellationToken cToken);
		Task<byte[]> ReceivePeerMessageAsync(Peer peer, CancellationToken cToken);
		Task DisconnectAllPeersAsync(CancellationToken cToken);
		Task DisconnectPeerAsync(Peer peer, CancellationToken cToken);
		void UpdatePeerStatesAsync(CancellationToken cToken);
		Task UpdatePeerSelectionAsync(CancellationToken cToken);
	}
}
