using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WiseTorrent.Utilities.Types;

namespace WiseTorrent.Storage.Interfaces
{
    public interface IFileManager
    {
        // Write block to disk using a provided file map
        public Task WriteBlockAsync(Block block, FileMap fileMap, CancellationToken cancellationToken);
    }
}
