using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiseTorrent.Pieces.Types
{
    internal class Piece
    {
        public int Index { get; }
        public byte[]? Data { get; set; }
        public bool State { get; set; }
        public string ExpectedHash { get; }

        public Piece(int index, string expectedHash)
        {
            Index = index;
            ExpectedHash = expectedHash;
            State = false;
        }

        public bool Validate()
        {
            if (Data == null) return false;

            using var sha1 = SHA1.Create();
            var hash = sha1.ComputeHash(Data);
            var actualHash = BitConverter.ToString(hash).Replace("-", "").ToLower();

            State = actualHash == ExpectedHash.ToLower();
            return State;
        }
    }
}
