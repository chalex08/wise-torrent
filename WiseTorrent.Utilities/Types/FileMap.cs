namespace WiseTorrent.Utilities.Types
{
	public class FileMap
	{
		private Dictionary<int, List<FileSegment>> pieceMap = new();
		private readonly long pieceLength;  // number of bytes in each piece

		public FileMap(long pieceLength, IEnumerable<TorrentFile> files)
		{
			this.pieceLength = pieceLength;
			BuildMap(files);
		}

		private void BuildMap(IEnumerable<TorrentFile> files)
		{
			long globalOffset = 0;
			int pieceIndex = 0;

			foreach (var file in files)
			{
				long remaining = file.Length.ConvertUnit(ByteUnit.Byte).Size;
				long fileOffset = 0;

				while (remaining > 0)
				{
					long assignLength = Math.Min(pieceLength - globalOffset % pieceLength, remaining);

					if (!pieceMap.TryGetValue(pieceIndex, out var segments))
					{
						segments = new List<FileSegment>();
						pieceMap[pieceIndex] = segments;
					}

					segments.Add(new FileSegment(Path.Join(SessionConfig.TorrentStoragePath, file.RelativePath), fileOffset, assignLength));

					remaining -= assignLength;
					fileOffset += assignLength;
					globalOffset += assignLength;

					// Move to next piece if the current piece is full
					if (globalOffset % pieceLength == 0)
					{
						pieceIndex++;
					}
				}
			}
		}

		public IReadOnlyList<FileSegment> Resolve(int pieceIndex)
		{
			if (!pieceMap.TryGetValue(pieceIndex, out var segments))
				throw new ArgumentOutOfRangeException(nameof(pieceIndex), "No mapping for this piece index.");

			return segments;
		}
	}
}
