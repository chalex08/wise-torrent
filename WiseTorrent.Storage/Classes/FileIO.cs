using WiseTorrent.Storage.Interfaces;

namespace WiseTorrent.Storage.Classes
{
	internal class FileIO : IFileIO
	{
		// Open the file for reading
		public async Task<int> ReadAsync(string filePath, byte[] buffer, long offset, int count, CancellationToken cancellationToken = default)
		{
			if (buffer == null)
				throw new ArgumentNullException(nameof(buffer), "Buffer cannot be null.");
			if (count < 0 || count > buffer.Length)
				throw new ArgumentOutOfRangeException(nameof(count));
			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset));

			using (var stream = new FileStream(
				filePath,
				FileMode.Open,
				FileAccess.Read,
				FileShare.Read,
				bufferSize: 4096,
				useAsync: true)
				)
			{
				stream.Seek(offset, SeekOrigin.Begin);
				return await stream.ReadAsync(buffer, 0, count, cancellationToken).ConfigureAwait(false);
			}
		}

		// Open the file for writing
		public async Task WriteAsync(string filePath, byte[] buffer, long offset, int count, CancellationToken cancellationToken = default, int bufferOffset = 0)
		{
			if (buffer == null)
				throw new ArgumentNullException(nameof(buffer), "Buffer cannot be null.");
			if (count < 0 || count > buffer.Length)
				throw new ArgumentOutOfRangeException(nameof(count));
			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset));

			using (var stream = new FileStream(
				filePath,
				FileMode.OpenOrCreate,
				FileAccess.Write,
				FileShare.Read,
				bufferSize: 4096,
				options: FileOptions.WriteThrough | FileOptions.Asynchronous))
			{
				stream.Seek(offset, SeekOrigin.Begin);
				await stream.WriteAsync(buffer, bufferOffset, count, cancellationToken).ConfigureAwait(false);
			}
		}

		// Delete a given file
		public async Task DeleteAsync(string filePath, CancellationToken cancellationToken = default)
		{
			await Task.Run(() =>
			{
				if (File.Exists(filePath))
					File.Delete(filePath);
			}, cancellationToken).ConfigureAwait(false);
		}
	}
}
