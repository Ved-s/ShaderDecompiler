using ShaderDecompiler.Structures;

namespace ShaderDecompiler.Decompiler.Expressions {
	public abstract class Expression {
		public abstract bool IsRegisterUsed(ParameterRegisterType type, uint index, bool? destination);
		public abstract string Decompile(ShaderDecompilationContext context);
		public abstract Expression Clone();

		public virtual bool SimplifyOnComplexityExceeded => false;

		public virtual Expression Simplify(ShaderDecompilationContext context, out bool fail) {
			fail = true;
			return this;
		}
		public virtual bool Clean(ShaderDecompilationContext context) => false;

		public virtual void MaskSwizzle(SwizzleMask mask) { }

		//public bool SafeSimplify(ShaderDecompilationContext context, out Expression result)
		//{
		//    Expression? expr = Simplify(context, out bool fail);
		//    if (fail)
		//        result = this;
		//    
		//    result = expr ?? throw new InvalidDataException("Expression simplified to nothing");
		//    return !fail;
		//}

		public virtual int CalculateComplexity() => 1;

	}
}
