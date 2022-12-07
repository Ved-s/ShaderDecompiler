#region License
/*
  ShaderDecompiler - Direct3D shader decompiler

  Released under Microsoft Public License
  See LICENSE for details
*/
#endregion

namespace ShaderDecompiler;

public struct BitNumber {
	private readonly uint Number;

	public bool this[int index] => ((Number >> index) & 1) != 0;
	public uint this[Range range] {
		get {
			int start = range.Start.IsFromEnd ? 0 : range.Start.Value;
			int end = range.End.IsFromEnd ? 31 : range.End.Value;

			int len = end - start + 1;
			uint mask = 0;
			for (int i = 0; i < len; i++)
				mask = (mask << 1) | 1;
			mask <<= start;
			return (Number & mask) >> start;
		}
	}

	public BitNumber(uint number) {
		Number = number;
	}
}
