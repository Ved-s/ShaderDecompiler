#region License
/*
  ShaderDecompiler - Direct3D shader decompiler

  Released under Microsoft Public License
  See LICENSE for details
*/
#endregion

using ShaderDecompiler.Decompilers.Expressions;
using ShaderDecompiler.Structures;

namespace ShaderDecompiler.Decompilers {
	public class ShaderDecompilationContext {
		public Shader Shader;
		public Dictionary<(ParameterRegisterType, uint), string> RegisterNames = new();
		public ShaderScanResult Scan = new();

		public List<Expression?> Expressions = new();
		public int CurrentExpressionIndex = 0;
		public DecompilationSettings Settings;

		public ShaderDecompilationContext(Shader shader, DecompilationSettings? settings = null) {
			Shader = shader;
			Settings = settings ?? new();
		}
	}
}
