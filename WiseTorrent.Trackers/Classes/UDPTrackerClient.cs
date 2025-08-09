using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using WiseTorrent.Trackers.Interfaces;

namespace WiseTorrent.Trackers.Classes
{
	internal class UDPTrackerClient : ITrackerClient
    {
	    private readonly UdpClient _udpClient;

	    public UDPTrackerClient()
	    {
		    _udpClient = new UdpClient();
	    }

	    public void InitialiseClient(string baseUri)
	    {
	    }
	}
}
