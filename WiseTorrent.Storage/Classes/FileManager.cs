using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using WiseTorrent.Storage.Interfaces;

namespace WiseTorrent.Storage.Classes
{
    public class FileManager
    {
        private readonly IDiskAllocator _diskAllocator;
        private readonly IFileIO _fileIO;
        private readonly CancellationToken _cancellationToken;

        private ConcurrentQueue<Piece> pieceQueue = new();
        private readonly int maxPieceQueueSize;

        private readonly Task _workerTask;

        public FileManager(IDiskAllocator diskAllocator, IFileIO fileIO, CancellationToken cancellationToken, int maxPieceQueueSize = 20)
        {
            _diskAllocator = diskAllocator;
            _fileIO = fileIO;
            _cancellationToken = cancellationToken;
            this.maxPieceQueueSize = maxPieceQueueSize;
        }

        // Queue piece for later writing
        public void ProcessPieceAsync(Piece piece)
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

                if (!_diskAllocator.VerifyAllocation(piece.FilePath))
                {
                    // TODO: piece.FilePath doesn't exist use FileMap
                    // TODO: DONT use piece.Data.Length, get whole file size from FileMap
                    await _diskAllocator.Allocate(piece.FilePath, piece.Data.Length, _cancellationToken);
                }

                // TODO: replace piece.FilePath and piece.Offset with FileMap 
                await _fileIO.WriteAsync(
                    piece.FilePath,
                    piece.Data,
                    piece.Offset,
                    piece.Data.Length,
                    _cancellationToken);
            }
        }
    }
}
