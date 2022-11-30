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
        public readonly float[] Values;

        public ConstantExpression(float[] values)
        {
            Values = values;
        }

        public override string Decompile(ShaderDecompilationContext context)
        {
            if (Values.Length == 1)
                return Values[0].ToString(CultureInfo.InvariantCulture);

            return $"float{Values.Length}({string.Join(", ", Values.Select(f => f.ToString(CultureInfo.InvariantCulture)))})";
        }

        public override bool IsRegisterUsed(ParameterRegisterType type, uint index) => false;

        public override string ToString()
        {
            return $"float{Values.Length}({string.Join(", ", Values.Select(f => f.ToString(CultureInfo.InvariantCulture)))})";
        }

        public override Expression Clone()
        {
            float[] copy = new float[Values.Length];
            Array.Copy(Values, copy, Values.Length);
            return new ConstantExpression(copy);
        }
    }
}
