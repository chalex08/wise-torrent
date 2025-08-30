namespace WiseTorrent.Utilities.Types
{
	public record FileSegment(string FilePath, long Offset, long Length);
}
