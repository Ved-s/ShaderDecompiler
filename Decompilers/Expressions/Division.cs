#region License
/*
  ShaderDecompiler - Direct3D shader decompiler

  Released under Microsoft Public License
  See LICENSE for details
*/
#endregion

namespace ShaderDecompiler.Decompilers.Expressions {
	public class DivisionExpression : MathOperationExpression {
		public DivisionExpression() : base('/') {
		}
	}
}
