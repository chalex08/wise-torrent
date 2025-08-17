namespace WiseTorrent.Peers.Interfaces
{
	public interface IPeerServiceTaskClient
	{
		Task StartServiceTask(CancellationToken cToken);
	}
}