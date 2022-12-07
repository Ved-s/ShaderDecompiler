#region License
/*
  ShaderDecompiler - Direct3D shader decompiler

  Released under Microsoft Public License
  See LICENSE for details
*/
#endregion

namespace ShaderDecompiler.Structures;

public enum ObjectType : uint {
	Void,
	Bool,
	Int,
	Float,
	String,
	Texture,
	Texture1d,
	Texture2d,
	Texture3d,
	Texturecube,
	Sampler,
	Sampler1d,
	Sampler2d,
	Sampler3d,
	Samplercube,
	PixelShader,
	VertexShader,
	PixelFragment,
	VertexFragment,
	Unsupported,
}
