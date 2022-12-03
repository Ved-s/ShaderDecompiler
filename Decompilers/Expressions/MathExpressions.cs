namespace ShaderDecompiler.Decompilers.Expressions {
	public abstract class MathOperationExpression : ComplexExpression {
		private readonly char Operation;

		public MathOperationExpression(char operation) {
			Operation = operation;
		}

		public Expression A => SubExpressions[0];
		public Expression B => SubExpressions[1];
		public override ValueCheck<int> ArgumentCount => 2;

		public override string Decompile(ShaderDecompilationContext context) {

			// TODO: Expression.NeedsParenthesesWrapping
			string a = A.Decompile(context);
			string b = B.Decompile(context);

			if (A is MathOperationExpression && A.GetType() != GetType()) a = $"({a})";
			if (B is MathOperationExpression && B.GetType() != GetType()) b = $"({b})";

			return $"{a} {Operation} {b}";
		}

		public override string ToString() {
			return $"({A}) {Operation} {B}";
		}
	}
}
