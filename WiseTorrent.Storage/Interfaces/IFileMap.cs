using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WiseTorrent.Storage.Types;

namespace WiseTorrent.Storage.Interfaces
{
    public interface IFileMap
    {
        IReadOnlyList<FileSegment> Resolve(int pieceIndex);
    }
}
