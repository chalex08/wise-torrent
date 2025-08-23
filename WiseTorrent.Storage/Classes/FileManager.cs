using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WiseTorrent.Pieces.Types;
using WiseTorrent.Storage.Interfaces;
using WiseTorrent.Storage.Types;

namespace WiseTorrent.Storage.Classes
{
    public class FileManager
    {
        private readonly IDiskAllocator _diskAllocator;
        private readonly IFileIO _fileIO;

        public FileManager(IDiskAllocator diskAllocator, IFileIO fileIO)
        {
            _diskAllocator = diskAllocator;
            _fileIO = fileIO;
        }

        public async Task ProcessPieceAsync(Piece piece, FileMap fileMap, CancellationToken cancellationToken)
        {
            try
            {
                var segments = fileMap.Resolve(piece.Index);
                var firstSegmentOffset = segments[0].Offset;

                foreach (var segment in segments)
                {
                    // Ensure file allocation for current segment
                    if (!_diskAllocator.VerifyAllocation(segment.FilePath, segment.Offset + segment.Length))
                    {
                        await _diskAllocator.Allocate(segment.FilePath, segment.Offset + segment.Length, cancellationToken);
                    }

                    // Write the segment to disk
                    var pieceOffset = (int)(segment.Offset - firstSegmentOffset);

                    await _fileIO.WriteAsync(
                        segment.FilePath,
                        piece.Data!,
                        segment.Offset,
                        (int)segment.Length,
                        cancellationToken,
                        pieceOffset);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
