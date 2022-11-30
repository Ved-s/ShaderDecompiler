namespace ShaderDecompiler.Decompiler.Expressions
{
    public abstract class MathExpressionExpression : ComplexExpression {
		private readonly char sym;

		public MathExpressionExpression(char s) {
			sym = s;
		}

		public Expression A => SubExpressions[0];
		public Expression B => SubExpressions[1];
		public override int ArgumentCount => 2;

		public override string Decompile(ShaderDecompilationContext context) {
			return $"{A.Decompile(context)} {sym} {B.Decompile(context)}";
		}

		public override string ToString() {
			return $"{A} {sym} {B}";
		}
	}
	public class MultiplyExpression : MathExpressionExpression {
		public MultiplyExpression() : base('*') {
		}
	}
	public class AddExpression : MathExpressionExpression {
		public AddExpression() : base('+') {
		}
	}
	public class SubstractExpression : MathExpressionExpression {
		public SubstractExpression() : base('-') {
		}
	}
}
