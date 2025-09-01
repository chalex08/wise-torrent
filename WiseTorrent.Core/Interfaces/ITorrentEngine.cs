namespace WiseTorrent.Core.Interfaces
{
	public interface ITorrentEngine
	{
		Task CancelTorrentEngineSession(string torrentName);
		Task PauseTorrentEngineSession(string filePath);
		void StartTorrentEngineSession(string filePath);
	}
}
