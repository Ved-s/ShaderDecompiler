using ShaderDecompiler.Structures;
using System.Diagnostics;
using System.Diagnostics.Tracing;

namespace ShaderDecompiler.Decompiler.Expressions
{
    public class RegisterExpression : Expression
    {
        public ParameterRegisterType Type;
        public uint Index;
        public Swizzle? X;
        public Swizzle? Y;
        public Swizzle? Z;
        public Swizzle? W;
        public bool Destination;

        public bool FullRegister => X == Swizzle.X && Y == Swizzle.Y && Z == Swizzle.Z && W == Swizzle.W;
        public SwizzleMask WriteMask 
        {
            get 
            {
                if (!Destination)
                    throw new InvalidOperationException("Register is not destination");

                SwizzleMask mask = SwizzleMask.None;

                if (X.HasValue) mask |= SwizzleMask.X;
                if (Y.HasValue) mask |= SwizzleMask.Y;
                if (Z.HasValue) mask |= SwizzleMask.Z;
                if (W.HasValue) mask |= SwizzleMask.W;

                return mask;
            }
        }

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

        public override bool IsRegisterUsed(ParameterRegisterType type, uint index, bool? destination)
        {
            return type == Type && index == Index && (destination is null || destination == Destination);
        }

        public override string Decompile(ShaderDecompilationContext context)
        {
            if (FullRegister)
                return GetName(context);

            //if (X.HasValue && Y.HasValue && Z.HasValue && W.HasValue && X.Value == Y.Value && Y.Value == Z.Value && Z.Value == W.Value)
            //    return $"{GetName(context)}.{X?.ToString().ToLower()}";

            return $"{GetName(context)}.{X?.ToString().ToLower()}{Y?.ToString().ToLower()}{Z?.ToString().ToLower()}{W?.ToString().ToLower()}";
        }

        public string GetName(ShaderDecompilationContext context)
        {
            if (!context.RegisterNames.TryGetValue((Type, Index), out string? name))
                name = $"{Type.ToString().ToLower()}{Index}";
            return name;
        }

        public bool IsSameRegisterAs(RegisterExpression expr)
            => expr.Type == Type
            && expr.Index == Index;

        public bool IsExactRegisterAs(RegisterExpression expr)
        {
            if (!IsSameRegisterAs(expr))
                return false;

            HashSet<Swizzle> swizzle = new();

            // did this, so a.z (Z___ swizzle) and a.z (___Z swizzle) count as same

            if (X.HasValue) swizzle.Add(X.Value);
            if (Y.HasValue) swizzle.Add(Y.Value);
            if (Z.HasValue) swizzle.Add(Z.Value);
            if (W.HasValue) swizzle.Add(W.Value);

            if (expr.X.HasValue) swizzle.Remove(expr.X.Value);
            if (expr.Y.HasValue) swizzle.Remove(expr.Y.Value);
            if (expr.Z.HasValue) swizzle.Remove(expr.Z.Value);
            if (expr.W.HasValue) swizzle.Remove(expr.W.Value);

            return swizzle.Count == 0;
        }

        public override Expression Simplify(ShaderDecompilationContext context, out bool fail)
        {
            fail = true;

            if (Destination)
                return this;

            // If this register is used inbetween this expression and next assignment (including current expression) to the register or end
            for (int i = context.CurrentExpressionIndex; i < context.Expressions.Count; i++)
            {
                if (context.Expressions[i] is null)
                    continue;

                if (context.Expressions[i] is AssignExpression assign && assign.Destination.IsExactRegisterAs(this))
                    break;

                bool used = i != context.CurrentExpressionIndex && context.Expressions[i]!.IsRegisterUsed(Type, Index, false);
                if (used)
                    return this;
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

                    if (i != context.CurrentExpressionIndex && context.Expressions[i]!.IsRegisterUsed(Type, Index, false))
                        return this;
                }

            if (assignment is not null)
            {
                if (context.Expressions[context.CurrentExpressionIndex]!.CalculateWeight() - CalculateWeight() + assignment.CalculateWeight() > context.SimplificationWeightThreshold)
                    return this;

                fail = false;
                return assignment.Clone();
            }
            return this;
        }

        public override Expression Clone()
        {
            return new RegisterExpression(Type, Index, X, Y, Z, W, Destination);
        }

        public override void MaskSwizzle(SwizzleMask mask)
        {
            if (!mask.HasFlag(SwizzleMask.X)) X = null;
            if (!mask.HasFlag(SwizzleMask.Y)) Y = null;
            if (!mask.HasFlag(SwizzleMask.Z)) Z = null;
            if (!mask.HasFlag(SwizzleMask.W)) W = null;
        }

        public override string ToString()
        {
            return $"{Type.ToString().ToLower()}{Index}.{X?.ToString().ToLower() ?? "_"}{Y?.ToString().ToLower() ?? "_"}{Z?.ToString().ToLower() ?? "_"}{W?.ToString().ToLower() ?? "_"}";
        }
    }
}
