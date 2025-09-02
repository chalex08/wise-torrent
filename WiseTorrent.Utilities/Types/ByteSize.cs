using System.Text.Json.Serialization;

namespace WiseTorrent.Utilities.Types
{
	public class ByteSize
	{
		public ByteUnit Unit { get; }
		public long Size { get; }

		[JsonConstructor]
		public ByteSize(ByteUnit unit, long size)
		{
			if (size < 0) throw new ArgumentOutOfRangeException(nameof(size));
			Unit = unit;
			Size = size;
		}

		public ByteSize(long sizeInBytes)
		{
			if (sizeInBytes < 0) throw new ArgumentOutOfRangeException(nameof(sizeInBytes));
			if (sizeInBytes < (long)ByteUnit.Kibibyte) Unit = ByteUnit.Byte;
			else if (sizeInBytes < (long)ByteUnit.Mebibyte) Unit = ByteUnit.Kibibyte;
			else Unit = ByteUnit.Mebibyte;
			Size = sizeInBytes / (long)Unit;
		}

		public ByteSize ConvertUnit(ByteUnit newUnit)
		{
			return new ByteSize(newUnit, Size * (long)Unit / (long)newUnit);
		}

		public static ByteSize NormaliseByteSize(long sizeInBytes)
		{
			ByteUnit unit;
			if (sizeInBytes < 0) throw new ArgumentOutOfRangeException(nameof(sizeInBytes));
			if (sizeInBytes < (long)ByteUnit.Kibibyte) unit = ByteUnit.Byte;
			else if (sizeInBytes < (long)ByteUnit.Mebibyte) unit = ByteUnit.Kibibyte;
			else unit = ByteUnit.Mebibyte;
			return new ByteSize(unit, sizeInBytes / (long)unit);
		}

		public bool Equals(ByteSize compare)
		{
			return Unit == compare.Unit && Size.Equals(compare.Size);
		}

		public override string ToString()
		{
			return $"{Size:D3} {Unit.ToString()}";
		}

		public static ByteSize operator +(ByteSize s1, ByteSize s2)
		{
			return NormaliseByteSize(s1.ConvertUnit(ByteUnit.Byte).Size + s2.ConvertUnit(ByteUnit.Byte).Size);
		}
	}
}