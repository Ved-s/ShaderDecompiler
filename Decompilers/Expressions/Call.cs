#region License
/*
  ShaderDecompiler - Direct3D shader decompiler

  Released under Microsoft Public License
  See LICENSE for details
*/
#endregion

namespace ShaderDecompiler.Decompilers.Expressions {
	public class CallExpression : ComplexExpression {
		public string FunctionName = null!;
		public override ValueCheck<int> ArgumentCount => ValueCheck<int>.Any;

		public CallExpression(string functionName, params Expression[] arguments) {
			FunctionName = functionName;
			SubExpressions = arguments;
		}

		public override ComplexExpression CloneSelf() {
			return new CallExpression(FunctionName);
		}

		public override string Decompile(ShaderDecompilationContext context) {
			return $"{FunctionName}({string.Join(", ", SubExpressions.Select(arg => arg.Decompile(context)))})";
		}

		public override SwizzleMask ModifySubSwizzleMask(SwizzleMask mask, int subIndex) {

			if (FunctionName == "tex2D" && subIndex == 1)
				return SwizzleMask.X | SwizzleMask.Y;

			return mask;
		}

		public override string ToString() {
			return $"{FunctionName}({string.Join(", ", (object[])SubExpressions)})";
		}
	}
}
