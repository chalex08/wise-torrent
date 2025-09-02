using WiseTorrent.Utilities.Types;

namespace WiseTorrent.Storage.Interfaces
{
	public interface IFileManager
	{
		// Write block to disk using a provided file map
		public Task WriteBlockAsync(Block block, FileMap fileMap, CancellationToken cancellationToken);
		// Read block from disk using a provided file map
		Task<byte[]> ReadBlockAsync(Block block, FileMap fileMap, CancellationToken cancellationToken);
	}
}
