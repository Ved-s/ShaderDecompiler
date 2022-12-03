using ShaderDecompiler.Structures;

namespace ShaderDecompiler.Decompilers.Expressions {
	public abstract class Expression {

		public abstract string Decompile(ShaderDecompilationContext context);
		public abstract Expression Clone();

		public abstract SwizzleMask GetRegisterUsage(ParameterRegisterType type, uint index, bool? destination);

		public virtual Expression Simplify(ShaderDecompilationContext context, bool allowComplexityIncrease, out bool fail) {
			fail = true;
			return this;
		}
		public virtual bool Clean(ShaderDecompilationContext context) => false;

		public virtual void MaskSwizzle(SwizzleMask mask) { }

		public virtual int CalculateComplexity() => 1;

		public static Expression operator +(Expression a, Expression b) => ComplexExpression.Create<AdditionExpression>(a, b);
		public static Expression operator -(Expression a, Expression b) => ComplexExpression.Create<SubtractionExpression>(a, b);
		public static Expression operator *(Expression a, Expression b) => ComplexExpression.Create<MultiplicationExpression>(a, b);
		public static Expression operator /(Expression a, Expression b) => ComplexExpression.Create<DivisionExpression>(a, b);

		public static Expression operator -(Expression a) => ComplexExpression.Create<DivisionExpression>(a);
	}
}
