using System.Collections.Concurrent;
using WiseTorrent.Storage.Interfaces;
using WiseTorrent.Utilities.Interfaces;
using WiseTorrent.Utilities.Types;

namespace WiseTorrent.Storage.Classes
{
	internal class StorageServiceTaskClient : IStorageServiceTaskClient
	{
		private readonly ILogger<StorageServiceTaskClient> _logger;
		private readonly IFileManager _fileManager;

		private CancellationToken CToken { get; set; }
		private readonly ConcurrentQueue<Block> _queue = new();
		private readonly SemaphoreSlim _signal = new(0);

		public StorageServiceTaskClient(ILogger<StorageServiceTaskClient> logger, IFileManager fileManager)
		{
			_logger = logger;
			_fileManager = fileManager;
		}

		public async Task StartServiceTask(TorrentSession torrentSession, CancellationToken cToken)
		{
			CToken = cToken;
			_logger.Info("Storage service task started");

			torrentSession.OnBlockReceived.Subscribe(block =>
			{
				if (TryEnqueue(block))
					_logger.Info($"Queued new block for writing to disk (Piece Index, Block Offset: {block.PieceIndex}, {block.Offset})");
				else
					_logger.Error($"Failed to queue new block (Piece Index, Block Offset: {block.PieceIndex}, {block.Offset})");
			});

			var fileMap = torrentSession.FileMap;

			while (!CToken.IsCancellationRequested)
			{
				var block = await DequeueAsync(CToken);
				if (block == null) continue;

				await _fileManager.WriteBlockAsync(block, fileMap, CancellationToken.None);
				torrentSession.RemainingBytes -= block.Length;
			}

			if (torrentSession.ShouldFlushOnShutdown)
			{
				int flushCount = await FlushRemainingPiecesAsync(torrentSession);
				_logger.Info($"Storage service task flushed remaining {flushCount} blocks");
				torrentSession.OnPiecesFlushed.NotifyListeners(true);
			}
			
			_logger.Info("Storage service task stopped");
		}

		private bool TryEnqueue(Block block)
		{
			_queue.Enqueue(block);

			_signal.Release();
			return true;
		}

		private async Task<Block?> DequeueAsync(CancellationToken token)
		{
			await _signal.WaitAsync(token);


			if (_queue.TryDequeue(out var block))
			{
				_logger.Info($"Dequeued block for writing to disk (Piece Index, Block Offset: {block.PieceIndex}, {block.Offset})");
				return block;
			}

			return null;
		}

		private async Task<int> FlushRemainingPiecesAsync(TorrentSession torrentSession)
		{
			int flushCount = 0;
			while (!_queue.IsEmpty)
			{
				var block = await DequeueAsync(CancellationToken.None);
				if (block == null) continue;

				await _fileManager.WriteBlockAsync(block, torrentSession.FileMap, CancellationToken.None);
				torrentSession.RemainingBytes -= block.Length;
				flushCount++;
			}

			return flushCount;
		}
	}
}
