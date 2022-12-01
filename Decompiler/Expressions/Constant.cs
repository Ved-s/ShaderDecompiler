using ShaderDecompiler.Structures;
using System.Globalization;

namespace ShaderDecompiler.Decompiler.Expressions {
	public class ConstantExpression : Expression {
		public readonly float Value;

		public ConstantExpression(float value) {
			Value = value;
		}

		public override string Decompile(ShaderDecompilationContext context) {
			return Value.ToString(CultureInfo.InvariantCulture);
		}

		public override bool IsRegisterUsed(ParameterRegisterType type, uint index, bool? destination) => false;

		public override string ToString() {
			return Value.ToString(CultureInfo.InvariantCulture);
		}

		public override Expression Clone() {
			return new ConstantExpression(Value);
		}
	}
}
