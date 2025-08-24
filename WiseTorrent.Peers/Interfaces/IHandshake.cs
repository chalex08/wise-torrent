namespace WiseTorrent.Peers.Interfaces
{
	public interface IHandshake
	{
        byte[] CreateHandshake(byte[] infoHash, string peerId);

        bool TryParseHandshake(byte[] data, out string infoHash, out string peerId);

        bool IsValidHandshake(string receivedInfoHash, string expectedInfoHash);
    }
}
