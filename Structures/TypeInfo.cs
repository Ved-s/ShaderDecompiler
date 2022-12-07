#region License
/*
  ShaderDecompiler - Direct3D shader decompiler

  Released under Microsoft Public License
  See LICENSE for details
*/
#endregion

namespace ShaderDecompiler.Structures;

public class TypeInfo {
	public ObjectClass Class;
	public ObjectType Type;
	public uint Rows;
	public uint Columns;
	public uint Elements;

	public Value[] StructMembers = Array.Empty<Value>();

	public uint GetSize(out uint actualSize) {
		switch (Class) {
			case ObjectClass.MatrixRows:
				actualSize = Rows * Columns;
				return 4 * Rows;

			case ObjectClass.MatrixColumns:
				actualSize = Rows * Columns;
				return Columns * 4;

			case ObjectClass.Struct: {
					uint size = 0;
					actualSize = 0;

					foreach (var member in StructMembers) {
						size += Math.Max(4, member.Type.GetSize(out uint memberActualSize));

						actualSize += memberActualSize;
					}
					return size;
				}

			case ObjectClass.Scalar:
				actualSize = 1;
				return 1;

			case ObjectClass.Vector:
				actualSize = Columns;
				return Columns;

			case ObjectClass.Object:
				actualSize = 0;
				return 0;
		}

		actualSize = 0;
		return 0;
	}

	public bool TransformTypeDefaultValueDataPos(uint dataPos, out uint arrayPos) {
		arrayPos = 0;
		switch (Class) {
			case ObjectClass.Struct: {
					uint posOffset = 0;
					uint actualOffset = 0;
					foreach (var member in StructMembers) {
						uint memberSize = 4;
						memberSize = Math.Max(memberSize, member.Type.GetSize(out uint memberActualSize));
						if (memberSize + posOffset > dataPos) {
							bool result = member.Type.TransformTypeDefaultValueDataPos(dataPos - posOffset, out arrayPos);
							arrayPos += actualOffset;
							return result;
						}

						posOffset += memberSize;
						actualOffset += memberActualSize;
					}

					return false;
				}

			case ObjectClass.MatrixRows:
			case ObjectClass.MatrixColumns: {
					uint row = dataPos % 4;
					if (row >= Rows)
						return false;

					uint col = dataPos / 4;
					if (col >= Columns)
						return false;

					arrayPos = row * Columns + col;
					return true;
				}

			case ObjectClass.Scalar:
				arrayPos = 0;
				return dataPos == 0;

			case ObjectClass.Vector:
				arrayPos = dataPos;
				return dataPos < Columns;

			case ObjectClass.Object:
				return false;

		}
		return false;
	}

	public override string ToString() {
		string name = Type switch {
			ObjectType.Void => "void",
			ObjectType.Bool => "bool",
			ObjectType.Int => "int",
			ObjectType.Float => "float",
			ObjectType.String => "string",
			ObjectType.Texture => "texture",
			ObjectType.Texture1d => "texture1D",
			ObjectType.Texture2d => "texture2D",
			ObjectType.Texture3d => "texture3D",
			ObjectType.Texturecube => "texture_cube",
			ObjectType.Sampler => "sampler",
			ObjectType.Sampler1d => "sampler1D",
			ObjectType.Sampler2d => "sampler2D",
			ObjectType.Sampler3d => "sampler3D",
			ObjectType.Samplercube => "sampler_cube",
			ObjectType.PixelShader => "PixelShader",
			ObjectType.VertexShader => "VertexShader",
			ObjectType.PixelFragment => "PixelFragment",
			ObjectType.VertexFragment => "VertexFragment",
			_ => "unknown"
		};

		switch (Class) {
			case ObjectClass.Object:
			case ObjectClass.Scalar:
				break;

			case ObjectClass.Vector:
				name += Columns;
				break;

			case ObjectClass.MatrixRows:
			case ObjectClass.MatrixColumns:
				name = $"{name}{Rows}x{Columns}";
				break;

			case ObjectClass.Struct:
				name = $"struct {{\n{string.Join('\n', StructMembers.Select(m => $"\t{m.ToString().Replace("\n", "\n\t")}"))}\n}}";
				break;
		}

		if (Elements > 1)
			name += $"[{Elements}]";

		return name;
	}
}
