#region License
/*
  ShaderDecompiler - Direct3D shader decompiler

  Released under Microsoft Public License
  See LICENSE for details
*/
#endregion

using ShaderDecompiler.Structures;
using System.Globalization;

namespace ShaderDecompiler.Decompilers.Expressions {
	public class ConstantExpression : Expression {
		public readonly float Value;

		public ConstantExpression(float value) {
			Value = value;
		}

		public override string Decompile(ShaderDecompilationContext context) {
			return Value.ToString(CultureInfo.InvariantCulture);
		}

		public override IEnumerable<RegisterExpression> EnumerateRegisters() {
			yield break;
		}

		public override SwizzleMask GetRegisterUsage(ParameterRegisterType type, uint index, bool? destination) => SwizzleMask.None;

		public override string ToString() {
			return Value.ToString(CultureInfo.InvariantCulture);
		}

		public override Expression Clone() {
			return new ConstantExpression(Value);
		}
	}
}
