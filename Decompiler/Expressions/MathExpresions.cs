namespace ShaderDecompiler.Decompiler.Expressions
{
    public abstract class MathOperationExpression : ComplexExpression {
		private readonly char Operation;

		public MathOperationExpression(char operation) {
			Operation = operation;
		}

		public Expression A => SubExpressions[0];
		public Expression B => SubExpressions[1];
		public override ValueCheck<int> ArgumentCount => 2;

		public override string Decompile(ShaderDecompilationContext context) {
			return $"{A.Decompile(context)} {Operation} {B.Decompile(context)}";
		}

		public override string ToString() {
			return $"{A} {Operation} {B}";
		}
	}

	public class MultiplicationExpression : MathOperationExpression {
		public MultiplicationExpression() : base('*') {
		}
	}

	public class AddExpression : MathOperationExpression {
		public AddExpression() : base('+') {
		}
	}

	public class SubstractExpression : MathOperationExpression {
		public SubstractExpression() : base('-') {
		}
	}

    public class DivisionExpression : MathOperationExpression
    {
        public DivisionExpression() : base('/') {
		}
    }
}
