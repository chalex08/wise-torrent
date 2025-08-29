namespace WiseTorrent.Core.Interfaces
{
	public interface ITorrentEngine
	{
		Task CancelTorrentEngineSession(byte[] infoHash);
		void StartTorrentEngineSession(string filePath);
	}
}
