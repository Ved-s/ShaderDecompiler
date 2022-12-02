using ShaderDecompiler.Structures;

namespace ShaderDecompiler.Decompiler.Expressions {
	public abstract class Expression {

		public virtual bool SimplifyOnComplexityExceeded => false;

		public abstract string Decompile(ShaderDecompilationContext context);
		public abstract Expression Clone();

		public abstract SwizzleMask GetRegisterUsage(ParameterRegisterType type, uint index, bool? destination);

		public virtual Expression Simplify(ShaderDecompilationContext context, out bool fail) {
			fail = true;
			return this;
		}
		public virtual bool Clean(ShaderDecompilationContext context) => false;

		public virtual void MaskSwizzle(SwizzleMask mask) { }

		public virtual int CalculateComplexity() => 1;
	}
}
