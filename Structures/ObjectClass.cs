#region License
/*
  ShaderDecompiler - Direct3D shader decompiler

  Released under Microsoft Public License
  See LICENSE for details
*/
#endregion

namespace ShaderDecompiler.Structures;

public enum ObjectClass : uint {
	Scalar,
	Vector,
	MatrixRows,
	MatrixColumns,
	Object,
	Struct,
}
