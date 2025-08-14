using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WiseTorrent.Peers.Types;

namespace WiseTorrent.Trackers.Interfaces
{
    public interface ITrackerClient
    {
	    Task<(int, bool)> RunServiceTask(int interval, string trackerAddress, Action<List<Peer>> onTrackerResponse, CancellationToken cancellationToken);
    }
}
