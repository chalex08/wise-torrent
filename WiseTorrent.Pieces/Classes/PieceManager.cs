using System.Collections;
using System.Reflection.Metadata.Ecma335;
using WiseTorrent.Pieces.Interfaces;
using WiseTorrent.Utilities.Types;

namespace WiseTorrent.Pieces.Classes
{
	public class PieceManager : IPieceManager
	{
		private readonly BitArray _localBitfield; // true = have, false = missing
		private readonly int _totalPieces;
		private readonly Dictionary<int, int> _pieceRarity = new();

		public PieceManager(int totalPieces)
		{
			_totalPieces = totalPieces;
			_localBitfield = new BitArray(totalPieces);
		}

		public IEnumerable<int> GetMissingPieces()
		{
			for (int i = 0; i < _totalPieces; i++)
			{
				if (!_localBitfield[i])
					yield return i;
			}
		}

		public void MarkPieceComplete(int index)
		{
			_localBitfield[index] = true;
		}

		public bool HasPiece(int index) => _localBitfield[index];

		public void UpdatePieceRarityFromPeer(HashSet<int> peerPieces)
		{
			for (int i = 0; i < peerPieces.Count; i++)
			{
				if (peerPieces.Contains(i))
				{
					_pieceRarity[i] = _pieceRarity.TryGetValue(i, out var count)
						? count + 1
						: 1;
				}
			}
		}

		public void RemovePeerFromRarity(HashSet<int> peerPieces)
		{
			for (int i = 0; i < peerPieces.Count; i++)
			{
				if (peerPieces.Contains(i) && _pieceRarity.TryGetValue(i, out var count))
				{
					_pieceRarity[i] = Math.Max(0, count - 1);
				}
			}
		}

		public IEnumerable<int> GetRarestPieces(IEnumerable<int> candidates)
		{
			return candidates
				.OrderBy(p => _pieceRarity.GetValueOrDefault(p, int.MaxValue));
		}

		public IEnumerable<int> GetRarePieces(IEnumerable<int> candidates)
		{
			return candidates.Where(p => _pieceRarity.GetValueOrDefault(p, int.MaxValue) <= SessionConfig.PieceRarityThreshold);
		}

		public bool HasAllPieces() => _localBitfield.HasAllSet();

		public PieceManagerSnapshot CreateSnapshot()
		{
			var bitfieldList = new List<bool>(_totalPieces);
			for (int i = 0; i < _totalPieces; i++)
				bitfieldList.Add(_localBitfield[i]);

			return new PieceManagerSnapshot
			{
				TotalPieces = _totalPieces,
				LocalBitfield = bitfieldList
			};
		}

		public static PieceManager RestoreFromSnapshot(PieceManagerSnapshot snapshot)
		{
			var manager = new PieceManager(snapshot.TotalPieces);
			for (int i = 0; i < snapshot.TotalPieces; i++)
				manager._localBitfield[i] = snapshot.LocalBitfield[i];
			return manager;
		}

	}
}
