#region License
/*
  ShaderDecompiler - Direct3D shader decompiler

  Released under Microsoft Public License
  See LICENSE for details
*/
#endregion

namespace ShaderDecompiler.Decompilers {
	public enum DeclUsage : uint {
		Position = 0,
		BlendWeight = 1,
		BlendIndices = 2,
		Normal = 3,
		Psize = 4,
		Texcoord = 5,
		Tangent = 6,
		Binormal = 7,
		Tessfactor = 8,
		Positiont = 9,
		Color = 10,
		Fog = 11,
		Depth = 12,
		Sample = 13,
		Unknown = 14
	}
}
