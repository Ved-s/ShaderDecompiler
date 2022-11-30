using ShaderDecompiler.Structures;

namespace ShaderDecompiler.Decompiler.Expressions
{
    public abstract class ComplexExpression : Expression
    {
        public abstract IEnumerable<Expression> EnumerateSubExpressions();

        public override bool IsRegisterUsed(ParameterRegisterType type, uint index)
        {
            return EnumerateSubExpressions().Any(expr => expr.IsRegisterUsed(type, index));
        }
    }
}
