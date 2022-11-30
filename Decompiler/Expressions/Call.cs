using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShaderDecompiler.Decompiler.Expressions
{
    public class CallExpression : ComplexExpression
    {
        public readonly string FunctionName;
        public readonly Expression[] Arguments;

        public CallExpression(string functionName, params Expression[] arguments)
        {
            FunctionName = functionName;
            Arguments = arguments;
        }

        public override string Decompile(ShaderDecompilationContext context)
        {
            return $"{FunctionName}({string.Join(", ", Arguments.Select(arg => arg.Decompile(context)))})";
        }

        public override IEnumerable<Expression> EnumerateSubExpressions()
        {
            foreach (var expression in Arguments)
                yield return expression;
        }

        public override Expression? Simplify(ShaderDecompilationContext context, out bool fail)
        {
            fail = true;
            for (int i = 0; i < Arguments.Length; i++)
                fail &= !Arguments[i].SafeSimplify(context, out Arguments[i]);
            return this;
        }
    }
}
