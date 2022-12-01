using ShaderDecompiler.Structures;

namespace ShaderDecompiler.Decompiler {
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
