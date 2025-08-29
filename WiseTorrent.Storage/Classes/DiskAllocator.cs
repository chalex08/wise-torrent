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
        public async Task Allocate(string filePath, long requiredSize, CancellationToken cancellationToken = default)
        {
            if (requiredSize < 1)
                return;

            long currentSize = 0;

            if (File.Exists(filePath))
            {
                var info = new FileInfo(filePath);
                currentSize = info.Length;

                if (currentSize >= requiredSize)
                {
                    return; // File large enough
                }
            }


            var buffer = new byte[1];
            await _fileIO.WriteAsync(filePath, buffer, requiredSize - 1, 1, cancellationToken);
        }

        // Deallocate disk space from a file
        public async Task Deallocate(string filePath, CancellationToken cancellationToken = default)
        {
            await _fileIO.DeleteAsync(filePath, cancellationToken);
        }

        // Verify disk space allocation for a file
        public bool VerifyAllocation(string filePath, long requiredSize)
        {
            if (!File.Exists(filePath))
            {
                return false;
            }

            var info = new FileInfo(filePath);
            return info.Length >= requiredSize;
        }
    }
}