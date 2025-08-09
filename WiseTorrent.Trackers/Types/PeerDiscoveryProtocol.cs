using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiseTorrent.Trackers.Types
{
    public enum PeerDiscoveryProtocol
    {
        HTTP,
        HTTPS,
        UDP,
        DHT,
        WS,
        INVALID
    }
}
