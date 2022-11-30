namespace ShaderDecompiler.Decompiler.Expressions
{
    public class MultiplyExpression : ComplexExpression
    {
        public Expression A;
        public Expression B;

        public MultiplyExpression(Expression a, Expression b)
        {
            A = a;
            B = b;
        }

        public override string Decompile(ShaderDecompilationContext context)
        {
            return $"{A.Decompile(context)} * {B.Decompile(context)}";
        }

        public override IEnumerable<Expression> EnumerateSubExpressions()
        {
            yield return A;
            yield return B;
        }

        public override Expression? Simplify(ShaderDecompilationContext context, out bool fail)
        {
            fail = true;
            fail &= !A.SafeSimplify(context, out A);
            fail &= !B.SafeSimplify(context, out B);
            return this;
        }
    }
}
