using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShaderDecompiler.Structures
{
    public abstract class AnnotatedObject
    {
        public Value[] Annotations = Array.Empty<Value>();
    }
}
