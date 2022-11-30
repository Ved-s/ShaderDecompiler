using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShaderDecompiler.Structures
{
    public class Parameter : AnnotatedObject
    {
        public uint Flags;
        public Value Value = null!;

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
