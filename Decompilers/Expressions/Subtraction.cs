#region License
/*
  ShaderDecompiler - Direct3D shader decompiler

  Released under Microsoft Public License
  See LICENSE for details
*/
#endregion

using System.Runtime.CompilerServices;

namespace ShaderDecompiler.Decompilers.Expressions {
	public class SubtractionExpression : MathOperationExpression {
		public SubtractionExpression() : base('-') {
		}
	}
}
