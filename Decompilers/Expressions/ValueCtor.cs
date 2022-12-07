#region License
/*
  ShaderDecompiler - Direct3D shader decompiler

  Released under Microsoft Public License
  See LICENSE for details
*/
#endregion

namespace ShaderDecompiler.Decompilers.Expressions {
	public class ValueCtorExpression : ComplexExpression {
		public override ValueCheck<int> ArgumentCount => new(v => v > 0 && v <= 4);

		public override string Decompile(ShaderDecompilationContext context) {
			return $"float{SubExpressions.Length}({string.Join(", ", SubExpressions.Select(expr => expr.Decompile(context)))})";
		}

		public override string ToString() {
			return $"float{SubExpressions.Length}({string.Join(", ", (object[])SubExpressions)})";
		}
	}
}
