using ShaderDecompiler.Decompiler.Expressions;
using ShaderDecompiler.Structures;

namespace ShaderDecompiler.Decompiler {
	public struct ShaderDecompilationContext {
		public Shader Shader;
		public Dictionary<(ParameterRegisterType, uint), string> RegisterNames = new();
		public ShaderScanResult Scan = default!;

		public List<Expression?> Expressions = new();
		public int CurrentExpressionIndex = 0;
		public bool CurrentExpressionTooComplex = false;

		public int ComplexityThreshold = 15000;

		public ShaderDecompilationContext(Shader shader) {
			Shader = shader;
		}
	}
}
