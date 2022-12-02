using System.Text;

namespace ShaderDecompiler.Structures;

public class SourceParameter {
	public static readonly Dictionary<ParameterRegisterType, string> RegisterTypeNames = new() {
		[ParameterRegisterType.Temp] = "tmp",
		[ParameterRegisterType.Input] = "arg",
		[ParameterRegisterType.Const] = "const",
		[ParameterRegisterType.Output] = "out",
	};
	public static readonly Dictionary<Swizzle, string> SwizzleNames = new() {
		[Swizzle.X] = "x",
		[Swizzle.Y] = "y",
		[Swizzle.Z] = "z",
		[Swizzle.W] = "w",
	};

	public uint Register;
	public ParameterRegisterType RegisterType;
	public bool RelativeAddressing;
	public SourceModifier Modifier;

	public Swizzle SwizzleX;
	public Swizzle SwizzleY;
	public Swizzle SwizzleZ;
	public Swizzle SwizzleW;

	public static SourceParameter Read(BinaryReader reader, ShaderVersion version) {
		SourceParameter param = new();
		BitNumber token = new(reader.ReadUInt32());

		param.Register = token[0..10];
		param.RegisterType = (ParameterRegisterType)(token[11..12] << 3 | token[28..30]);
		param.RelativeAddressing = token[13];
		param.SwizzleX = (Swizzle)token[16..17];
		param.SwizzleY = (Swizzle)token[18..19];
		param.SwizzleZ = (Swizzle)token[20..21];
		param.SwizzleW = (Swizzle)token[22..23];
		param.Modifier = (SourceModifier)token[24..27];

		if (param.RegisterType == ParameterRegisterType.Address && version.PixelShader is true)
			param.RegisterType = ParameterRegisterType.Texture;

		if (param.RegisterType == ParameterRegisterType.Output && version.PixelShader is false && version.Major < 3)
			param.RegisterType = ParameterRegisterType.Texcrdout;

		return param;
	}

	public override string ToString() {
		StringBuilder sb = new();

		if (!RegisterTypeNames.TryGetValue(RegisterType, out string? name))
			name = RegisterType.ToString().ToLower();

		string registerName = $"{name}{Register}";

		sb.Append(registerName);

		if (SwizzleX != Swizzle.X || SwizzleY != Swizzle.Y || SwizzleZ != Swizzle.Z || SwizzleW != Swizzle.W) {
			sb.Append('.');

			if (SwizzleX == SwizzleY && SwizzleY == SwizzleZ && SwizzleZ == SwizzleW) {
				sb.Append(SwizzleNames[SwizzleX]);
			}
			else {
				sb.Append(SwizzleNames[SwizzleX]);
				sb.Append(SwizzleNames[SwizzleY]);
				sb.Append(SwizzleNames[SwizzleZ]);
				sb.Append(SwizzleNames[SwizzleW]);
			}
		}
		string result = sb.ToString();

		return Modifier switch {
			SourceModifier.Negate => $"-{result}",
			SourceModifier.Bias => $"{result} - 0.5",
			SourceModifier.BiasNegate => $"-({result} - 0.5)",
			SourceModifier.Sign => $"sign({result})",
			SourceModifier.SignNegate => $"-sign({result})",
			SourceModifier.Complement => $"1 - {result}",
			SourceModifier.Double => $"{result} * 2",
			SourceModifier.DoubleNegate => $"{result} * -2",
			SourceModifier.DivideByZ => $"{result} / {registerName}.z",
			SourceModifier.DivideByW => $"{result} / {registerName}.w",
			SourceModifier.Abs => $"abs({result})",
			SourceModifier.AbsNegate => $"-abs({result})",
			SourceModifier.LogicalNot => $"!{result}",
			_ => result
		};
	}
}
