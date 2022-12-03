namespace ShaderDecompiler.Decompilers.Expressions {
	public class AdditionExpression : MathOperationExpression {
		public AdditionExpression() : base('+') {
		}

		public override Expression SimplifySelf(ShaderDecompilationContext context, bool allowComplexityIncrease, out bool fail) {
			fail = true;

			// -a + -b
			if (MatchExpressions(out NegateExpression? negateA, out NegateExpression? negateB)) { 
				fail = false;
				return -(negateA.Expression + negateB.Expression);
			}

			// -a + b or a + -b
			if (MatchExpressions(out NegateExpression? negate, out Expression? expr)) {
				fail = false;
				return expr - negate.Expression;
			}

			// (s * (b - a)) + a -> lerp()
			if (MatchExpressions(out MultiplicationExpression? mul, out RegisterExpression? regA1)
			 && mul.MatchExpressions(out Expression? exprS, out SubtractionExpression? sub)
			 && sub.MatchExpressions(out Expression? exprB, out RegisterExpression? regA2)
			 && regA1.IsExactRegisterAs(regA2)) {
				fail = false;
				return new CallExpression("lerp", regA1, exprB, exprS);
			}

			return this;
		}
	}
}
