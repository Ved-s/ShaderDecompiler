using ShaderDecompiler.Decompiler.Expressions;
using ShaderDecompiler.Structures;

namespace ShaderDecompiler.Decompiler
{
    public static class DecompilerExtensions
    {
        public static RegisterExpression ToExpr(this DestinationParameter dest)
        {
            return new RegisterExpression(
                dest.RegisterType,
                dest.Register,
                dest.WriteX ? Swizzle.X : null,
                dest.WriteY ? Swizzle.Y : null,
                dest.WriteZ ? Swizzle.Z : null,
                dest.WriteW ? Swizzle.W : null,
                true);
        }

        public static Expression ToExpr(this SourceParameter src)
        {
            RegisterExpression reg = new(
                src.RegisterType,
                src.Register,
                src.SwizzleX,
                src.SwizzleY,
                src.SwizzleZ,
                src.SwizzleW,
                false);

            switch (src.Modifier)
            {
                case SourceModifier.None:
                    return reg;

                case SourceModifier.Negate:
                    return ComplexExpression.Create<NegateExpression>(reg);

                case SourceModifier.Abs:
                    return new CallExpression("abs", reg);

                default:
                    throw new NotImplementedException();
            }

        }

        public static AssignExpression Assign(this RegisterExpression regexpr, Expression expr) 
            => ComplexExpression.Create<AssignExpression>(regexpr, expr);
    }
}
