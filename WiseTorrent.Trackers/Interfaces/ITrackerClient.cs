using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiseTorrent.Trackers.Interfaces
{
    public interface ITrackerClient
    {
	    void InitialiseClient(string baseUri);
    }
}
