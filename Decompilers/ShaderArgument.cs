#region License
/*
  ShaderDecompiler - Direct3D shader decompiler

  Released under Microsoft Public License
  See LICENSE for details
*/
#endregion

using ShaderDecompiler.Structures;

namespace ShaderDecompiler.Decompilers {
	public class ShaderArgument {
		public ParameterRegisterType RegisterType;
		public uint Register;
		public DeclUsage Usage;
		public uint UsageIndex;
		public uint Size;
		public bool Input;
		public bool Output;
	}
}
