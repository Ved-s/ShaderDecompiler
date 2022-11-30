namespace ShaderDecompiler.Decompiler.Expressions
{
    public class AssignExpression : ComplexExpression
    {
        public readonly RegisterExpression Destination;
        public Expression Source;

        public AssignExpression(RegisterExpression destination, Expression source)
        {
            Destination = destination;
            Source = source;
        }

        public override string Decompile(ShaderDecompilationContext context)
        {
            bool needsType = context.Scan.Arguments.All(arg => arg.RegisterType != Destination.Type || arg.Register != Destination.Index)
                && (context.CurrentExpressionIndex == 0 || context.Expressions
                    .Take(context.CurrentExpressionIndex)
                    .All(expr => expr is not AssignExpression assign || !assign.Destination.IsSameRegisterAs(Destination)));

            string type = "";
            if (needsType)
            {
                if (!context.Scan.RegisterSizes.TryGetValue((Destination.Type, Destination.Index), out uint size))
                    size = 4;

                if (size > 1)
                    type = "float" + size + " ";
                else
                    type = "float ";
            }

            return $"{type}{Destination.Decompile(context)} = {Source.Decompile(context)}";
        }

        public override IEnumerable<Expression> EnumerateSubExpressions()
        {
            yield return Destination;
            yield return Source;
        }

        public override Expression? Simplify(ShaderDecompilationContext context, out bool fail)
        {
            fail = true;
            fail &= !Source.SafeSimplify(context, out Source);

            if (!context.Scan.RegistersReferenced.Contains((Destination.Type, Destination.Index, false)))
            {
                // Don't remove registers that weren't used in the first place
                return this;
            }


            for (int i = context.CurrentExpressionIndex + 1; i < context.Expressions.Count; i++)
            {
                bool used = i != context.CurrentExpressionIndex && context.Expressions[i].IsRegisterUsed(Destination.Type, Destination.Index);
                if (used)
                    return this;

                if (context.Expressions[i] is AssignExpression assign && assign.Destination.IsSameRegisterAs(Destination))
                    break;
            }
            // Register isn't used later
            fail = false;
            return null;
        }
    }
}
