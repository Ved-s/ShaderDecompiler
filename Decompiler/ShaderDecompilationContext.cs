using ShaderDecompiler.Decompiler.Expressions;
using ShaderDecompiler.Structures;

namespace ShaderDecompiler.Decompiler {
	public class ShaderDecompilationContext {
		public Shader Shader;
		public Dictionary<(ParameterRegisterType, uint), string> RegisterNames = new();
		public ShaderScanResult Scan = new();

		public List<Expression?> Expressions = new();
		public int CurrentExpressionIndex = 0;

		public int ComplexityThreshold = 20;

		public ShaderDecompilationContext(Shader shader) {
			Shader = shader;
		}
	}
}
