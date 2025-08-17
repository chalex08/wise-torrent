using WiseTorrent.Storage.Interfaces;

namespace WiseTorrent.Storage.Classes
{
    public class FileIO : IFileIO
    {
        // Open the file for reading
        public async Task<int> ReadAsync(string filePath, byte[] buffer, long offset, int count, CancellationToken cancellationToken = default)
        {
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
        public async Task WriteAsync(string filePath, byte[] buffer, long offset, int count, CancellationToken cancellationToken = default)
        {
            using (var stream = new FileStream(
                       filePath,
                       FileMode.OpenOrCreate,
                       FileAccess.Write,
                       FileShare.None,
                       bufferSize: 4096,
                       useAsync: true))
            {
                stream.Seek(offset, SeekOrigin.Begin);
                await stream.WriteAsync(buffer, 0, count, cancellationToken).ConfigureAwait(false);
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

        // Open the file and flush its contents to disk
        public async Task FlushAsync(string filePath, CancellationToken cancellationToken = default)
        {
            using (var stream = new FileStream(
                       filePath,
                       FileMode.Open,
                       FileAccess.Write,
                       FileShare.None,
                       bufferSize: 4096,
                       useAsync: true))
            {
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
