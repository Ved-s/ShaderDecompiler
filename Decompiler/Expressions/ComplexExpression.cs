using ShaderDecompiler.Structures;
using System.Diagnostics;

namespace ShaderDecompiler.Decompiler.Expressions
{
    public abstract class ComplexExpression : Expression
    {
        public Expression[] SubExpressions = Array.Empty<Expression>();
        public abstract ValueCheck<int> ArgumentCount { get; }

        static Stack<Expression> RegCheckStack = new();

        public static T Create<T>(params Expression[] expressions) where T : ComplexExpression, new()
        {
            if (typeof(T) == typeof(AssignExpression) && expressions[0] is not RegisterExpression)
                Debugger.Break();

            T expr = new();
            if (!expr.ArgumentCount.Check(expressions.Length))
                throw new ArgumentException("Wrong parameter count", nameof(expressions));
            expr.SubExpressions = expressions;
            return expr;
        }

        public override bool IsRegisterUsed(ParameterRegisterType type, uint index)
        {

            RegCheckStack.Push(this);
            bool res = SubExpressions.Any(expr => expr.IsRegisterUsed(type, index));
            RegCheckStack.Pop();
            return res;
        }

        public sealed override Expression Simplify(ShaderDecompilationContext context, out bool fail)
        {
            fail = true;

            for (int i = 0; i < SubExpressions.Length; i++)
            {
                if (context.CurrentExpressionExceedsWeight && !SubExpressions[i].SimplifyOnWeightExceeded)
                    continue;

                if (CalculateWeight() + SubExpressions[i].CalculateWeight() < context.SimplificationWeightThreshold)
                {
                    SubExpressions[i] = SubExpressions[i].Simplify(context, out bool exprFail);
                    fail &= exprFail;
                }
            }
            
            Expression expr = SimplifySelf(context, out bool selfFail);
            fail &= selfFail;
            return expr;
        }
        
        public sealed override Expression Clone()
        {
            ComplexExpression expr = CloneSelf();
            expr.SubExpressions = new Expression[SubExpressions.Length];
            for (int i = 0; i < expr.SubExpressions.Length; i++)
                expr.SubExpressions[i] = SubExpressions[i].Clone();
            return expr;
        }

        public override int CalculateWeight()
        {
            return 1 + SubExpressions.Sum(expr => expr.CalculateWeight());
        }

        public virtual ComplexExpression CloneSelf() => (ComplexExpression)Activator.CreateInstance(GetType())!;

        public virtual Expression SimplifySelf(ShaderDecompilationContext context, out bool fail)
        {
            fail = true;
            return Clone();
        }

    }
}
