namespace ShaderDecompiler.Decompiler.Expressions
{
    public class DivisionExpression : ComplexExpression
    {
        public Expression A => SubExpressions[0];
        public Expression B => SubExpressions[1];
        public override ValueCheck<int> ArgumentCount => 2;

        public override string Decompile(ShaderDecompilationContext context)
        {
            return $"{A.Decompile(context)} / {B.Decompile(context)}";
        }

        public override string ToString()
        {
            return $"{A} / {B}";
        }
    }
}
