using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiseTorrent.Storage.Interfaces
{
	public interface IDiskAllocator
	{
        // Allocate disk space for a file
        // file -> File path to allocate space for
        // fileSize -> Size of the file to allocate space for
        Task Allocate(string filePath, long fileSize, CancellationToken cancellationToken = default);

        // Deallocate disk space for a file
        // filePath -> File path to deallocate space for
        Task Deallocate(string filePath, CancellationToken cancellationToken = default);

        // Check if a file has allocated disk space
        // filePath -> File path to check allocation for
        bool VerifyAllocation(string filePath, long requiredSize);
    }
}
