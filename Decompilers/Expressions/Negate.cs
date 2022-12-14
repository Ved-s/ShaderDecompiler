#region License
/*
  ShaderDecompiler - Direct3D shader decompiler

  Released under Microsoft Public License
  See LICENSE for details
*/
#endregion

namespace ShaderDecompiler.Decompilers.Expressions {
	public class NegateExpression : ComplexExpression {
		public Expression Expression => SubExpressions[0];
		public override ValueCheck<int> ArgumentCount => 1;

		public override string Decompile(ShaderDecompilationContext context) {
			bool needsWrap = Expression is ComplexExpression and not CallExpression;
			if (needsWrap)
				return $"-({Expression.Decompile(context)})";
			return $"-{Expression.Decompile(context)}";
		}


		public override Expression SimplifySelf(ShaderDecompilationContext context, bool allowComplexityIncrease, out bool fail) {
			fail = true;

			if (Expression is SubtractionExpression sub) {
				fail = false;
				return Create<SubtractionExpression>(sub.B, sub.A);
			}

			return this;
		}

		public override string ToString() {
			bool needsWrap = Expression is ComplexExpression and not CallExpression;
			if (needsWrap)
				return $"-({Expression})";
			return $"-{Expression}";
		}
	}
}
