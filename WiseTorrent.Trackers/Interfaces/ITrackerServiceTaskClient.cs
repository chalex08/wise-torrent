using WiseTorrent.Core.Types;

namespace WiseTorrent.Trackers.Interfaces
{
	public interface ITrackerServiceTaskClient
	{
		Task StartServiceTask();
		void StopServiceTask();
	}
}
