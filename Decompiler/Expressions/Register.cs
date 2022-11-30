using ShaderDecompiler.Structures;

namespace ShaderDecompiler.Decompiler.Expressions
{
    public class RegisterExpression : Expression
    {
        public readonly ParameterRegisterType Type;
        public readonly uint Index;
        public readonly Swizzle? X;
        public readonly Swizzle? Y;
        public readonly Swizzle? Z;
        public readonly Swizzle? W;
        public readonly bool Destination;

        public RegisterExpression(ParameterRegisterType type, uint index, Swizzle? x, Swizzle? y, Swizzle? z, Swizzle? w, bool destination)
        {
            Type = type;
            Index = index;
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public override bool IsRegisterUsed(ParameterRegisterType type, uint index)
        {
            return type == Type && index == Index;
        }

        public override string Decompile(ShaderDecompilationContext context)
        {
            if (!context.RegisterNames.TryGetValue((Type, Index), out string? name))
                name = $"{Type.ToString().ToLower()}{Index}";

            if (X == Swizzle.X && Y == Swizzle.Y && Z == Swizzle.Z && W == Swizzle.W)
                return name;

            if (X.HasValue && Y.HasValue && Z.HasValue && W.HasValue && X.Value == Y.Value && Y.Value == Z.Value && Z.Value == W.Value)
                return $"{name}.{X?.ToString().ToLower()}";

            return $"{name}.{X?.ToString().ToLower()}{Y?.ToString().ToLower()}{Z?.ToString().ToLower()}{W?.ToString().ToLower()}";
        }

        public bool IsSameRegisterAs(RegisterExpression expr) => expr.Type == Type && expr.Index == Index;

        public override Expression? Simplify(ShaderDecompilationContext context, out bool fail)
        {
            fail = true;

            if (Destination)
                return this;

            // If this register is used inbetween this expression and next assignment (including current expression) to the register or end
            for (int i = context.CurrentExpressionIndex; i < context.Expressions.Count; i++)
            {
                if (context.Expressions[i] is AssignExpression assign && assign.Destination.IsSameRegisterAs(this))
                    break;

                bool used = i != context.CurrentExpressionIndex && context.Expressions[i].IsRegisterUsed(Type, Index);
                if (used)
                    return this;
            }

            Expression? assignment = null;

            // If this register is used inbetween this expression and prevoius assignment (excluding current expression) to the register or end
            if (context.CurrentExpressionIndex > 0)
                for (int i = context.CurrentExpressionIndex - 1; i >= 0; i--)
                {
                    if (context.Expressions[i] is AssignExpression assign && assign.Destination.IsSameRegisterAs(this))
                    {
                        assignment = assign.Source;
                        break;
                    }

                    if (context.Expressions[i].IsRegisterUsed(Type, Index))
                        return this;
                }

            if (assignment is not null)
            {
                fail = false;
                return assignment;
            }
            return this;
        }
    }
}
