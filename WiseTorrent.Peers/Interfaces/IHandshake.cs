using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiseTorrent.Peers.Interfaces
{
	public interface IHandshake
	{
        byte[] CreateHandshake(string infoHash, string peerId);

        bool TryParseHandshake(byte[] data, out string infoHash, out string peerId);

        bool IsValidHandshake(string receivedInfoHash, string expectedInfoHash);
    }
}
