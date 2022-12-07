#region License
/*
  ShaderDecompiler - Direct3D shader decompiler

  Released under Microsoft Public License
  See LICENSE for details
*/
#endregion

namespace ShaderDecompiler.Structures;

public enum SourceModifier : uint {
	None,
	Negate,
	Bias,
	BiasNegate,
	Sign,
	SignNegate,
	Complement,
	Double,
	DoubleNegate,
	DivideByZ,
	DivideByW,
	Abs,
	AbsNegate,
	LogicalNot
}
