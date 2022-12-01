﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShaderDecompiler.Decompiler.Expressions
{
    public class ValueCtorExpression : ComplexExpression
    {
        public override ValueCheck<int> ArgumentCount => new(v => v > 0 && v <= 4);

        public override string Decompile(ShaderDecompilationContext context)
        {
            return $"float{SubExpressions.Length}({string.Join(", ", SubExpressions.Select(expr => expr.Decompile(context)))})";
        }
    }
}
