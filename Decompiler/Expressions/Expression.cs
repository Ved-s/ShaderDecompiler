using ShaderDecompiler.Structures;

namespace ShaderDecompiler.Decompiler.Expressions
{
    public abstract class Expression
    {
        public abstract bool IsRegisterUsed(ParameterRegisterType type, uint index);
        public abstract string Decompile(ShaderDecompilationContext context);
        public abstract Expression Clone();

        public virtual bool SimplifyOnWeightExceeded => false;

        public virtual Expression Simplify(ShaderDecompilationContext context, out bool fail)
        {
            fail = true;
            return Clone();
        }
        public virtual bool Clean(ShaderDecompilationContext context) => false;

        //public bool SafeSimplify(ShaderDecompilationContext context, out Expression result)
        //{
        //    Expression? expr = Simplify(context, out bool fail);
        //    if (fail)
        //        result = this;
        //    
        //    result = expr ?? throw new InvalidDataException("Expression simplified to nothing");
        //    return !fail;
        //}

        public virtual int CalculateWeight() => 1;

    }
}
