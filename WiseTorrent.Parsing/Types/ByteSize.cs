using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiseTorrent.Parsing.Types
{
	public class ByteSize
	{
		public ByteUnit Unit;
		public double Size;

		public ByteSize(ByteUnit unit, double size)
		{
			if (size < 0) throw new ArgumentOutOfRangeException(nameof(size));
			this.Unit = unit;
			this.Size = size;
		}

		public ByteSize(double sizeInBytes)
		{
			if (sizeInBytes < 0) throw new ArgumentOutOfRangeException(nameof(sizeInBytes));
			else if (sizeInBytes < (long)ByteUnit.Kibibyte) Unit = ByteUnit.Byte;
			else if (sizeInBytes < (long)ByteUnit.Mebibyte) Unit = ByteUnit.Kibibyte;
			else Unit = ByteUnit.Mebibyte;
			Size = sizeInBytes / (long)Unit;
		}

		public void ConvertUnit(ByteUnit newUnit)
		{
			Size *= (long)Unit / (long)newUnit;
			Unit = newUnit;
		}

		public override string ToString()
		{
			return $"{Size:D3} {Unit.ToString()}";
		}
	}
}