using WiseTorrent.Storage.Interfaces;
using WiseTorrent.Utilities.Interfaces;
using WiseTorrent.Utilities.Types;

namespace WiseTorrent.Storage.Classes
{
	internal class FileManager : IFileManager
	{
		private readonly ILogger<FileManager> _logger;
		private readonly IDiskAllocator _diskAllocator;
		private readonly IFileIO _fileIO;

		public FileManager(ILogger<FileManager> logger, IDiskAllocator diskAllocator, IFileIO fileIO)
		{
			_logger = logger;
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

					if (block.Data == null || block.Data.All(b => b == 0))
						_logger.Warn($"Block data is null or zero-filled (Piece Index: {block.PieceIndex}, Offset: {block.Offset})");

					// Ensure file allocation
					if (!_diskAllocator.VerifyAllocation(segment.FilePath, writeStartInFile + writeLength))
					{
						await _diskAllocator.Allocate(
							segment.FilePath,
							writeStartInFile + writeLength,
							cancellationToken);
					}

					_logger.Info($"Attempting write to disk for block (Piece Index, Block Offset: {block.PieceIndex}, {block.Offset})");
					// Write data
					await _fileIO.WriteAsync(
						segment.FilePath,
						block.Data!,
						writeStartInFile,
						writeLength,
						cancellationToken,
						blockOffsetInData);
					_logger.Info($"Successful write to disk for block (Piece Index, Block Offset: {block.PieceIndex}, {block.Offset})");

					// Update trackers
					blockRemaining -= writeLength;
					blockOffsetInData += writeLength;
					pieceRelativeOffset += writeLength;
				}
			}
			catch (OperationCanceledException)
			{
			}
			catch (Exception ex)
			{
				_logger.Error($"Unsuccessful write to disk for block (Piece Index, Block Offset: {block.PieceIndex}, {block.Offset})", ex);
			}
		}
	}
}
