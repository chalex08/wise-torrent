using WiseTorrent.Parsing.Types;
using WiseTorrent.Peers.Types;

namespace WiseTorrent.Trackers.Interfaces
{
	public interface ITrackerServiceTaskClient
	{
		void InitialiseClient(List<ServerURL> trackerAddresses, Action<List<Peer>> onTrackerResponse);
		Task StartServiceTask();
		void StopServiceTask();
	}
}
