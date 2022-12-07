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

		public override void MaskSwizzle(SwizzleMask mask) {
			int size = mask.HasFlag(SwizzleMask.W) ? 4
					 : mask.HasFlag(SwizzleMask.Z) ? 3
					 : mask.HasFlag(SwizzleMask.Y) ? 2
					 : mask.HasFlag(SwizzleMask.X) ? 1 : 0;

			if (size > 0 && size < SubExpressions.Length) {
				Array.Resize(ref SubExpressions, size);
			}

			base.MaskSwizzle(mask);
		}

		public override string ToString() {
			return $"float{SubExpressions.Length}({string.Join(", ", (object[])SubExpressions)})";
		}
	}
}
