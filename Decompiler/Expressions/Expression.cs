using ShaderDecompiler.Structures;

namespace ShaderDecompiler.Decompiler.Expressions
{
    public abstract class Expression
    {
        public abstract bool IsRegisterUsed(ParameterRegisterType type, uint index);
        public abstract string Decompile(ShaderDecompilationContext context);

        public virtual Expression? Simplify(ShaderDecompilationContext context, out bool fail)
        {
            fail = true;
            return this;
        }

        public bool SafeSimplify(ShaderDecompilationContext context, out Expression result)
        {
            Expression? expr = Simplify(context, out bool fail);
            if (fail)
                result = this;
            
            result = expr ?? throw new InvalidDataException("Expression simplified to nothing");
            return !fail;
        }
    }
}
