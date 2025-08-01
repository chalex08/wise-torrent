using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiseTorrent.Parsing.Types
{
	public class ByteSize
	{
		public ByteUnit unit;
		public double size;

		public ByteSize(ByteUnit unit, double size)
		{
			if (size < 0) throw new ArgumentOutOfRangeException(nameof(size));
			this.unit = unit;
			this.size = size;
		}

		public ByteSize(double sizeInBytes)
		{
			if (sizeInBytes < 0) throw new ArgumentOutOfRangeException(nameof(sizeInBytes));
			else if (sizeInBytes < (long)ByteUnit.Kibibyte) unit = ByteUnit.Byte;
			else if (sizeInBytes < (long)ByteUnit.Mebibyte) unit = ByteUnit.Kibibyte;
			else unit = ByteUnit.Mebibyte;
			size = sizeInBytes / (long)unit;
		}

		public void ConvertUnit(ByteUnit newUnit)
		{
			size *= (long)unit / (long)newUnit;
			unit = newUnit;
		}

		public override string ToString()
		{
			return $"{size:D3} {unit.ToString()}";
		}
	}
}