using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WiseTorrent.Storage.Interfaces;
using WiseTorrent.Utilities.Types;

namespace WiseTorrent.Storage.Classes
{
    public class FileManager : IFileManager
    {
        private readonly IDiskAllocator _diskAllocator;
        private readonly IFileIO _fileIO;

        public FileManager(IDiskAllocator diskAllocator, IFileIO fileIO)
        {
            _diskAllocator = diskAllocator;
            _fileIO = fileIO;
        }
        
        public async Task WriteBlockAsync(Block block, FileMap fileMap, CancellationToken cancellationToken)
        {
            try
            {
                var segments = fileMap.Resolve(block.PieceIndex);

                int blockRemaining = block.Length;
                int blockOffsetInData = 0;  // How far we are in the block data
                long pieceRelativeOffset = 0;  // Track offset within piece

                foreach (var segment in segments)
                {
                    if (pieceRelativeOffset + segment.Length <= block.Offset)
                    {
                        pieceRelativeOffset += segment.Length;
                        continue;  // This segment is before the block offset
                    }

                    // If already written whole block, stop
                    if (blockRemaining <= 0)
                    {
                        break;
                    }

                    // Figure out where inside this segment the block intersects
                    long segmentOffsetWithinPiece = Math.Max(block.Offset - pieceRelativeOffset, 0);
                    long writeStartInFile = segment.Offset + segmentOffsetWithinPiece;

                    // How much can we write into this segment?
                    int writeLength = (int)Math.Min(
                        segment.Length - segmentOffsetWithinPiece,
                        blockRemaining);

                    // Ensure file allocation
                    if (!_diskAllocator.VerifyAllocation(segment.FilePath, writeStartInFile + writeLength))
                    {
                        await _diskAllocator.Allocate(
                            segment.FilePath,
                            writeStartInFile + writeLength,
                            cancellationToken);
                    }

                    // Write data
                    await _fileIO.WriteAsync(
                        segment.FilePath,
                        block.Data!,
                        writeStartInFile,
                        writeLength,
                        cancellationToken,
                        blockOffsetInData);

                    // Update trackers
                    blockRemaining -= writeLength;
                    blockOffsetInData += writeLength;
                    pieceRelativeOffset += writeLength;
                }
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
