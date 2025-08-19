using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WiseTorrent.Storage.Interfaces;
using WiseTorrent.Storage.Types;

namespace WiseTorrent.Storage.Classes
{
    public class FileMap : IFileMap
    {
        private Dictionary<int, List<FileSegment>> pieceMap = new();
        private readonly long pieceLength;  // number of bytes in each piece

        public FileMap(long pieceLength, IEnumerable<(string FilePath, long Length)> files)
        {
            this.pieceLength = pieceLength;
            BuildMap(files);
        }

        private void BuildMap(IEnumerable<(string FilePath, long Length)> files)
        {
            long globalOffset = 0;
            int pieceIndex = 0;

            foreach (var file in files)
            {
                long remaining = file.Length;
                long fileOffset = 0;

                while (remaining > 0)
                {
                    long assignLength = Math.Min(pieceLength - globalOffset % pieceLength, remaining);

                    if (!pieceMap.TryGetValue(pieceIndex, out var segments))
                    {
                        segments = new List<FileSegment>();
                        pieceMap[pieceIndex] = segments;
                    }

                    segments.Add(new FileSegment(file.FilePath, fileOffset, assignLength));

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
