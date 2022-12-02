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
