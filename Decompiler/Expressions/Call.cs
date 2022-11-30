﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShaderDecompiler.Decompiler.Expressions
{
    public class CallExpression : ComplexExpression
    {
        public string FunctionName = null!;
        public override int ArgumentCount => -1;

        public CallExpression(string functionName, params Expression[] arguments)
        {
            FunctionName = functionName;
            SubExpressions = arguments;
        }

        public override ComplexExpression CloneSelf()
        {
            return new CallExpression(FunctionName);
        }

        public override string Decompile(ShaderDecompilationContext context)
        {
            return $"{FunctionName}({string.Join(", ", SubExpressions.Select(arg => arg.Decompile(context)))})";
        }

        public override string ToString()
        {
            return $"{FunctionName}({string.Join(", ", (object[])SubExpressions)})";
        }
    }
}
