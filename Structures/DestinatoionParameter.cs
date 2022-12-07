#region License
/*
  ShaderDecompiler - Direct3D shader decompiler

  Released under Microsoft Public License
  See LICENSE for details
*/
#endregion

using System.Text;

namespace ShaderDecompiler.Structures;

public class DestinationParameter : OpcodeParameter {
	public bool WriteX;
	public bool WriteY;
	public bool WriteZ;
	public bool WriteW;

	public static DestinationParameter Read(BinaryReader reader, ShaderVersion version) {
		DestinationParameter param = new();
		BitNumber token = new(reader.ReadUInt32());

		param.Register = token[0..10];
		param.RegisterType = (ParameterRegisterType)(token[11..12] << 3 | token[28..30]);
		param.WriteX = token[16];
		param.WriteY = token[17];
		param.WriteZ = token[18];
		param.WriteW = token[19];

		if (param.RegisterType == ParameterRegisterType.Address && version.Type == ShaderType.PixelShader)
			param.RegisterType = ParameterRegisterType.Texture;

		if (param.RegisterType == ParameterRegisterType.Output && version.CheckVersionLess(ShaderType.VertexShader, 3, 0))
			param.RegisterType = ParameterRegisterType.Texcrdout;

		return param;
	}

	public override string ToString() {
		StringBuilder sb = new();

		if (SourceParameter.RegisterTypeNames.TryGetValue(RegisterType, out string? name))
			sb.Append(name);
		else
			sb.Append(RegisterType.ToString().ToLower());

		sb.Append(Register);

		if ((!WriteX || !WriteY || !WriteZ || !WriteW) && (WriteX || WriteY || WriteZ || WriteW)) {
			sb.Append('.');
			if (WriteX) sb.Append('x');
			if (WriteY) sb.Append('y');
			if (WriteZ) sb.Append('z');
			if (WriteW) sb.Append('w');
		}

		return sb.ToString();
	}
}
