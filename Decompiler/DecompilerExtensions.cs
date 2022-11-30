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
            if (src.Modifier != SourceModifier.None)
            {
                throw new NotImplementedException();
            }

            return new RegisterExpression(
                src.RegisterType,
                src.Register,
                src.SwizzleX,
                src.SwizzleY,
                src.SwizzleZ,
                src.SwizzleW,
                false);
        }

        public static AssignExpression Assign(this RegisterExpression regexpr, Expression expr) => new(regexpr, expr);
    }
}
