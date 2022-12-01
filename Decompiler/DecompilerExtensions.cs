using ShaderDecompiler.Decompiler.Expressions;
using ShaderDecompiler.Structures;
using System.ComponentModel;

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

        public static (TResult?, TArray?) GetTypeValue<TResult, TArray>(this TArray[] arr) where TResult : TArray
        {
            int index = -1;

            for (int i = 0; i < arr.Length; i++)
                if (arr[i] is TResult)
                {
                    index = i;
                    break;
                }

            if (index < 0)
                return (default, default);

            int otherIndex = (index + 1) % arr.Length;
            TArray? other = otherIndex == index ? default : arr[otherIndex];

            return ((TResult)arr[index], other);
        }
    }
}
