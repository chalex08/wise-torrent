namespace WiseTorrent.Utilities.Types
{
	public enum PeerProtocolStage
	{
		AwaitingHandshake,
		AwaitingBitfield,
		AwaitingHaveOrRequest,
		AwaitingPiece,
		Established
	}
}
