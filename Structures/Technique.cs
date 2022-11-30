using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShaderDecompiler.Structures
{
    public class Technique : AnnotatedObject
    {
        public string? Name;
        public Pass[] Passes = Array.Empty<Pass>();

        public override string ToString()
        {
            return $"technique {Name}\n{{\n{string.Join("\n", Passes.Select(p => "\t" + p.ToString()!.Replace("\n", "\n\t")))}\n}}";
        }
    }

    public class Pass : AnnotatedObject
    {
        public string? Name;
        public State[] States = Array.Empty<State>();

        public override string ToString()
        {
            return $"pass {Name}\n{{\n{string.Join("\n", States.Select(p => "\t" + p.ToString()!.Replace("\n", "\n\t")))}\n}}";
        }
    }

    public class State
    {
        public string? Name;
        public StateType Type;
        public Value Value = null!;

        public override string ToString()
        {
            return $"{Type};";
        }
    }

    public enum StateType 
    {
        VertexShader = 146,
        PixelShader = 147
    }
}
