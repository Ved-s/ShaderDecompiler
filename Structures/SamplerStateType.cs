#region License
/*
  ShaderDecompiler - Direct3D shader decompiler

  Released under Microsoft Public License
  See LICENSE for details
*/
#endregion

namespace ShaderDecompiler.Structures {
	public enum SamplerStateType : uint {
		Unknown0 = 0,
		Unknown1 = 1,
		Unknown2 = 2,
		Unknown3 = 3,
		Texture = 4,
		AddressU = 5,
		AddressV = 6,
		AddressW = 7,
		BorderColor = 8,
		MagFilter = 9,
		MinFilter = 10,
		MipFilter = 11,
		MipmapLODBias = 12,
		MaxMipLevel = 13,
		MaxAnisotropy = 14,
		SRGBTexture = 15,
		ElementIndex = 16,
		DMapOffset = 17
	}
}
