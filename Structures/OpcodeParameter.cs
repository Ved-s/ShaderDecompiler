#region License
/*
  ShaderDecompiler - Direct3D shader decompiler

  Released under Microsoft Public License
  See LICENSE for details
*/
#endregion

namespace ShaderDecompiler.Structures;

public abstract class OpcodeParameter {
	public uint Register;
	public ParameterRegisterType RegisterType;

}
