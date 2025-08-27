namespace WiseTorrent.Pieces.Interfaces
{
	public interface IPieceManager
	{
		IEnumerable<int> GetMissingPieces();
		void MarkPieceComplete(int index);
		bool HasPiece(int index);
		void UpdatePieceRarityFromPeer(HashSet<int> peerPieces);
		void RemovePeerFromRarity(HashSet<int> peerPieces);
		IEnumerable<int> GetRarestPieces(IEnumerable<int> candidates);
	}
}
