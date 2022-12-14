#region License
/*
  ShaderDecompiler - Direct3D shader decompiler

  Released under Microsoft Public License
  See LICENSE for details
*/
#endregion

namespace ShaderDecompiler.Structures {
	public enum ParameterRegisterType {
		Temp = 0,
		Input = 1,
		Const = 2,
		Address = 3,
		Rastout = 4,
		Attrout = 5,
		Output = 6,
		Constint = 7,
		Colorout = 8,
		Depthout = 9,
		Sampler = 10,
		Const2 = 11,
		Const3 = 12,
		Const4 = 13,
		Constbool = 14,
		Loop = 15,
		Tempfloat16 = 16,
		Misctype = 17,
		Label = 18,
		Predicate = 19,

		// Assigned manually
		Texcrdout = 100,
		Texture = 101,
		

		// Preshader register types
		PreshaderLiteral = 200,
		PreshaderInput = 201,
		PreshaderTemp = 202,
	}
}
