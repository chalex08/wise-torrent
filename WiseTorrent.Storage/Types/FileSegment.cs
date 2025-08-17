using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiseTorrent.Storage.Types
{
    public record FileSegment(string FilePath, long Offset, long Length);
}
