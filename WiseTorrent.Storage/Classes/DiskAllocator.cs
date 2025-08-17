using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WiseTorrent.Storage.Interfaces;

namespace WiseTorrent.Storage.Classes
{
    public class DiskAllocator : IDiskAllocator
    {
        private readonly IFileIO _fileIO;

        public DiskAllocator(IFileIO fileIO)
        {
            _fileIO = fileIO;
        }

        // Allocate disk space for a file
        public async Task Allocate(string filePath, long fileSize, CancellationToken cancellationToken = default)
        {
            if (fileSize < 1)
                return;

            var buffer = new byte[1];
            await _fileIO.WriteAsync(filePath, buffer, fileSize - 1, 0, cancellationToken);
        }

        // Deallocate disk space from a file
        public async Task Deallocate(string filePath, CancellationToken cancellationToken = default)
        {
            await _fileIO.DeleteAsync(filePath, cancellationToken);
        }

        // Verify disk space allocation for a file
        public bool VerifyAllocation(string filePath)
        {
            return File.Exists(filePath);
        }
    }
}