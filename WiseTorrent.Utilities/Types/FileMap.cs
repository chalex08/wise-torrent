using System.Text.Json.Serialization;

namespace WiseTorrent.Utilities.Types
{
	public class FileMap
	{
		public Dictionary<int, List<FileSegment>> PieceMap { get; private set; }
		public long PieceLength { get; }

		[JsonConstructor]
		public FileMap(long pieceLength, Dictionary<int, List<FileSegment>> pieceMap)
		{
			PieceLength = pieceLength;
			PieceMap = pieceMap;
		}

		public FileMap(long pieceLength, IEnumerable<TorrentFile> files)
		{
			PieceLength = pieceLength;
			PieceMap = new();
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
					long assignLength = Math.Min(PieceLength - globalOffset % PieceLength, remaining);

					if (!PieceMap.TryGetValue(pieceIndex, out var segments))
					{
						segments = new List<FileSegment>();
						PieceMap[pieceIndex] = segments;
					}

					segments.Add(new FileSegment(Path.Join(SessionConfig.TorrentStoragePath, file.RelativePath), fileOffset, assignLength));

					remaining -= assignLength;
					fileOffset += assignLength;
					globalOffset += assignLength;

					// Move to next piece if the current piece is full
					if (globalOffset % PieceLength == 0)
					{
						pieceIndex++;
					}
				}
			}
		}

		public IReadOnlyList<FileSegment> Resolve(int pieceIndex)
		{
			if (!PieceMap.TryGetValue(pieceIndex, out var segments))
				throw new ArgumentOutOfRangeException(nameof(pieceIndex), "No mapping for this piece index.");

			return segments;
		}
	}
}
