using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShaderDecompiler.Structures
{
    public class Value
    {
        public TypeInfo Type = new();
        public object? Object;
        public string? Name;
        public string? Semantic;

        public override string ToString()
        {
            return $"{Type} {Name}{(Semantic is null ? "" : $" : {Semantic}")};";
        }
    }
}
