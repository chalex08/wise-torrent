using WiseTorrent.Peers.Types;
using WiseTorrent.Trackers.Types;

namespace WiseTorrent.Trackers.Interfaces
{
    public interface ITrackerClient
    {
        void InitialiseClientState(byte[] infoHash, Peer userPeer, EventState eventState, int uploadCount, int downloadCount, long remainingBytes);

		Task<(int, bool)> RunServiceTask(int interval, string trackerAddress, Action<List<Peer>> onTrackerResponse, CancellationToken cancellationToken);
    }
}
