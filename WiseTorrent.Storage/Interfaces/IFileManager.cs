using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WiseTorrent.Pieces.Types;

namespace WiseTorrent.Storage.Interfaces
{
    public interface IFileManager
    {
        // Processes piece
        // This piece is added to a queue which are written to disk after a certain number of pieces are queued
        // piece -> Piece to process/add to queue
        public Task ProcessPieceAsync(Piece piece);

        // Flush all queued pieces to disk
        public Task FlushPiecesAsync();
    }
}
