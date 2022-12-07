#region License
/*
  ShaderDecompiler - Direct3D shader decompiler

  Released under Microsoft Public License
  See LICENSE for details
*/
#endregion

namespace ShaderDecompiler.Decompilers.Expressions {
	public class MultiplicationExpression : MathOperationExpression {
		public MultiplicationExpression() : base('*') {
		}

		public override Expression SimplifySelf(ShaderDecompilationContext context, bool allowComplexityIncrease, out bool fail) {
			fail = true;

			var (div, other) = SubExpressions.GetTypeValue<DivisionExpression, Expression>();
			if (div is not null && other is not null && div.A is ConstantExpression) {
				div.SubExpressions[0] = Create<MultiplicationExpression>(div.A, other);
				fail = false;
				return div;
			}

			(var @const, other) = SubExpressions.GetTypeValue<ConstantExpression, Expression>();
			if (@const is not null && other is not null && @const.Value == 1) {
				fail = false;
				return other;
			}

			return this;
		}
	}
}
