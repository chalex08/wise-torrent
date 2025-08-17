namespace WiseTorrent.Trackers.Interfaces
{
	public interface ITrackerServiceTaskClient
	{
		Task StartServiceTask(CancellationToken cToken);
	}
}
