using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShaderDecompiler.Structures
{
    public class SamplerState
    {
        public SamplerStateType Type;
        public Value Value = null!;
    }
}
