namespace ShaderDecompiler.Decompiler.Expressions {
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
			bool needsParentheses = A is MathOperationExpression and not AdditionExpression and not MultiplicationExpression;

			if (needsParentheses)
				return $"({A.Decompile(context)}) {Operation} {B.Decompile(context)}";

			return $"{A.Decompile(context)} {Operation} {B.Decompile(context)}";
		}

		public override string ToString() {
			return $"({A}) {Operation} {B}";
		}
	}
}
