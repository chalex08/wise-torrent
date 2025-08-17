using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using WiseTorrent.Storage.Interfaces;
using WiseTorrent.Storage.Types;

namespace WiseTorrent.Storage.Classes
{
    public class FileManager
    {
        private readonly IDiskAllocator _diskAllocator;
        private readonly IFileIO _fileIO;
        private FileMap _fileMap;
        private readonly CancellationToken _cancellationToken;

        private ConcurrentQueue<Piece> pieceQueue = new();
        private readonly int maxPieceQueueSize;

        private readonly Task _workerTask;

        public FileManager(IDiskAllocator diskAllocator, IFileIO fileIO, FileMap fileMap, CancellationToken cancellationToken, int maxPieceQueueSize = 20)
        {
            _diskAllocator = diskAllocator;
            _fileIO = fileIO;
            _fileMap = fileMap;
            _cancellationToken = cancellationToken;
            this.maxPieceQueueSize = maxPieceQueueSize;

            _workerTask = Task.Run(() => WorkerLoop(), _cancellationToken);
        }

        // Queue piece for later writing
        public void ProcessPiece(Piece piece)
        {
            pieceQueue.Enqueue(piece);
        }

        // Flush all queued pieces to disk
        public async Task FlushPiecesAsync()
        {
            await WriteBatchAsync();
        }

        // Background loop that periodically writes queued pieces in batches
        private async Task WorkerLoop()
        {
            try
            {
                while (!_cancellationToken.IsCancellationRequested)
                {
                    if (pieceQueue.Count >= maxPieceQueueSize)
                    {
                        await WriteBatchAsync();
                    }
                    else
                    {
                        await Task.Delay(100, _cancellationToken);
                    }
                }

                // Flush when shutting down
                await FlushPiecesAsync();
            }
            catch (OperationCanceledException)
            {
            }
        }

        // Dequeues pieces and writes them to disk
        private async Task WriteBatchAsync()
        {
            var piecesToWrite = new List<Piece>();

            while (pieceQueue.TryDequeue(out var piece) && piecesToWrite.Count < maxPieceQueueSize)
            {
                piecesToWrite.Add(piece);
            }

            foreach (var piece in piecesToWrite)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                var segments = _fileMap.Resolve(piece.Index);

                foreach (var segment in segments)
                {
                    // Ensure file allocation for at least current incoming segment
                    if (!_diskAllocator.VerifyAllocation(segment.FilePath))
                    {
                        await _diskAllocator.Allocate(segment.FilePath, segment.Offset + segment.Length, _cancellationToken);
                    }

                    // Write the segment to disk
                    var offsetInPiece = segment.Offset - segments[0].Offset;
                    var length = (int)segment.Length;
                    var dataSlice = new byte[length];
                    Array.Copy(piece.Data!, offsetInPiece, dataSlice, 0, length);

                    await _fileIO.WriteAsync(segment.FilePath, dataSlice, segment.Offset, length, _cancellationToken);
                }
            }
        }
    }
}
