using ShaderDecompiler.Structures;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShaderDecompiler.Decompiler.Expressions
{
    public class ConstantExpression : Expression
    {
        public readonly float Value;

        public ConstantExpression(float value)
        {
            Value = value;
        }

        public override string Decompile(ShaderDecompilationContext context)
        {
            return Value.ToString(CultureInfo.InvariantCulture);
        }

        public override bool IsRegisterUsed(ParameterRegisterType type, uint index) => false;

        public override string ToString()
        {
            return Value.ToString(CultureInfo.InvariantCulture);
        }

        public override Expression Clone()
        {
            return new ConstantExpression(Value);
        }
    }
}
