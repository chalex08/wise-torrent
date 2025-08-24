using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WiseTorrent.Pieces.Types;
using WiseTorrent.Storage.Types;

namespace WiseTorrent.Storage.Interfaces
{
    public interface IFileManager
    {
        // Write piece to disk using a provided file map
        public Task WritePieceAsync(Piece piece, FileMap fileMap, CancellationToken cancellationToken);
    }
}
