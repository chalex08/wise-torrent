namespace WiseTorrent.Core.Interfaces
{
	public interface ITorrentEngine
	{
		Task CancelTorrentEngineSession(string torrentName);
		void StartTorrentEngineSession(string filePath);
	}
}
