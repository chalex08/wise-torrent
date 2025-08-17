using BencodeNET.Objects;

namespace WiseTorrent.Parsing.Interfaces
{
	public interface IBEncodeReader
	{
		BDictionary? ParseTorrentFileFromPath(string path);
		Task<BDictionary?> ParseHttpTrackerResponseAsync(HttpResponseMessage response);
	}
}
