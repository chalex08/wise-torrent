namespace WiseTorrent.Storage.Interfaces
{
	public interface IFileIO
	{

		// Reads a sequence of bytes from a file asynchronously
		// filePath -> File path to read from.
		// buffer -> Buffer to fill.
		// offset -> Position in file to start reading.
		// count -> Number of bytes to read.
		Task<int> ReadAsync(
			string filePath,
			byte[] buffer,
			long offset,
			int count,
			CancellationToken cancellationToken = default);

		// Write a sequence of bytes to a file asynchronously
		// filePath -> File path to read from.
		// buffer -> Buffer to fill.
		// offset -> Position in file to start reading.
		// count -> Number of bytes to read.
		Task WriteAsync(
			string filePath,
			byte[] buffer,
			long offset,
			int count,
			CancellationToken cancellationToken = default,
			int bufferOffset = 0);

		// Delete a file asynchronously
		Task DeleteAsync(string filePath, CancellationToken cancellationToken = default);
	}
}
