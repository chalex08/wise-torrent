using WiseTorrent.Utilities.Types;

namespace WiseTorrent.Parsing.Interfaces
{
	public interface ITorrentParser
	{
		TorrentMetadata? ParseTorrentFileFromPath(string path);
	}
}
