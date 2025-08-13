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
	    void InitialiseClient(List<string> trackerAddresses, Action<List<Peer>> onTrackerResponse);
	    Task StartServiceTask();
	    void StopServiceTask();
    }
}
