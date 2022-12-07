#region License
/*
  ShaderDecompiler - Direct3D shader decompiler

  Released under Microsoft Public License
  See LICENSE for details
*/
#endregion

namespace ShaderDecompiler.Structures {
	public class Parameter : AnnotatedObject {
		public uint Flags;
		public Value Value = null!;

		public override string ToString() {
			return Value.ToString();
		}
	}
}
