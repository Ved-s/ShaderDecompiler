using ShaderDecompiler.Structures;
using System.Diagnostics;

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
            Destination = destination;
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

            //if (X.HasValue && Y.HasValue && Z.HasValue && W.HasValue && X.Value == Y.Value && Y.Value == Z.Value && Z.Value == W.Value)
            //    return $"{name}.{X?.ToString().ToLower()}";

            return $"{name}.{X?.ToString().ToLower()}{Y?.ToString().ToLower()}{Z?.ToString().ToLower()}{W?.ToString().ToLower()}";
        }

        public bool IsSameRegisterAs(RegisterExpression expr)
            => expr.Type == Type
            && expr.Index == Index;

        public bool IsExactRegisterAs(RegisterExpression expr)
        {
            if (!IsSameRegisterAs(expr))
                return false;

            return X == expr.X && Y == expr.Y && Z == expr.Z && W == expr.W;
        }

        public override Expression Simplify(ShaderDecompilationContext context, out bool fail)
        {
            fail = true;

            if (Destination)
                return Clone();

            // If this register is used inbetween this expression and next assignment (including current expression) to the register or end
            for (int i = context.CurrentExpressionIndex; i < context.Expressions.Count; i++)
            {
                if (context.Expressions[i] is null)
                    continue;

                //if (context.Expressions[i] is AssignExpression a && a.Destination.Type == ParameterRegisterType.Temp && Type == ParameterRegisterType.Temp)
                //    Debugger.Break();

                if (context.Expressions[i] is AssignExpression assign && assign.Destination.IsExactRegisterAs(this))
                    break;

                bool used = i != context.CurrentExpressionIndex && context.Expressions[i].IsRegisterUsed(Type, Index);
                if (used)
                    return Clone();
            }

            Expression? assignment = null;

            // If this register is used inbetween this expression and prevoius assignment (excluding current expression) to the register or end
            if (context.CurrentExpressionIndex > 0)
                for (int i = context.CurrentExpressionIndex - 1; i >= 0; i--)
                {
                    if (context.Expressions[i] is null)
                        continue;

                    if (context.Expressions[i] is AssignExpression assign && assign.Destination.IsExactRegisterAs(this))
                    {
                        context.Expressions[i] = null;
                        assignment = assign.Source;
                        break;
                    }

                    if (context.Expressions[i].IsRegisterUsed(Type, Index))
                        return Clone();
                }

            if (assignment is not null)
            {
                if (context.Expressions[context.CurrentExpressionIndex].CalculateWeight() - CalculateWeight() + assignment.CalculateWeight() > context.SimplificationWeightThreshold)
                    return Clone();

                fail = false;
                return assignment.Clone();
            }
            return Clone();
        }

        public override Expression Clone()
        {
            return new RegisterExpression(Type, Index, X, Y, Z, W, Destination);
        }

        public override string ToString()
        {
            return $"{Type.ToString().ToLower()}{Index}.{X?.ToString().ToLower()}{Y?.ToString().ToLower()}{Z?.ToString().ToLower()}{W?.ToString().ToLower()}";
        }
    }
}
