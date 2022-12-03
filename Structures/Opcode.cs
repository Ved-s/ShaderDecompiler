using System.Globalization;
using System.Text;

namespace ShaderDecompiler.Structures;

public struct Opcode {
	public static readonly Dictionary<OpcodeType, OpcodeTypeInfo> OpcodeInfo = new() {
		[OpcodeType.Nop] = new("nop"),
		[OpcodeType.Mov] = new("mov"),
		[OpcodeType.Add] = new("add"),
		[OpcodeType.Sub] = new("sub"),
		[OpcodeType.Mad] = new("mad"),
		[OpcodeType.Mul] = new("mul"),
		[OpcodeType.Rcp] = new("rcp"),
		[OpcodeType.Rsq] = new("rsq"),
		[OpcodeType.Dp3] = new("dp3"),
		[OpcodeType.Dp4] = new("dp4"),
		[OpcodeType.Min] = new("min"),
		[OpcodeType.Max] = new("max"),
		[OpcodeType.Slt] = new("slt"),
		[OpcodeType.Sge] = new("sge"),
		[OpcodeType.Exp] = new("exp"),
		[OpcodeType.Log] = new("log"),
		[OpcodeType.Lit] = new("lit"),
		[OpcodeType.Dst] = new("dst"),
		[OpcodeType.Lrp] = new("lrp"),
		[OpcodeType.Frc] = new("frc"),
		[OpcodeType.M4x4] = new("m4x4"),
		[OpcodeType.M4x3] = new("m4x3"),
		[OpcodeType.M3x4] = new("m3x4"),
		[OpcodeType.M3x3] = new("m3x3"),
		[OpcodeType.M3x2] = new("m3x2"),
		[OpcodeType.Call] = new("call"),
		[OpcodeType.Callnz] = new("callnz"),
		[OpcodeType.Loop] = new("loop"),
		[OpcodeType.Ret] = new("ret"),
		[OpcodeType.Endloop] = new("endloop"),
		[OpcodeType.Label] = new("label"),
		[OpcodeType.Dcl] = new("dcl"),
		[OpcodeType.Pow] = new("pow"),
		[OpcodeType.Crs] = new("crs"),
		[OpcodeType.Sgn] = new("sgn"),
		[OpcodeType.Abs] = new("abs"),
		[OpcodeType.Nrm] = new("nrm"),
		[OpcodeType.Sincos] = new("sincos"),
		[OpcodeType.Rep] = new("rep"),
		[OpcodeType.Endrep] = new("endrep"),
		[OpcodeType.If] = new("if", true),
		[OpcodeType.Ifc] = new("ifc", true),
		[OpcodeType.Else] = new("else", true),
		[OpcodeType.Endif] = new("endif", true),
		[OpcodeType.Break] = new("break", true),
		[OpcodeType.Breakc] = new("breakc", true),
		[OpcodeType.Mova] = new("mova"),
		[OpcodeType.Defb] = new("defb"),
		[OpcodeType.Defi] = new("defi"),
		[OpcodeType.Texcrd] = new("texcrd"),
		[OpcodeType.Texkill] = new("texkill"),
		[OpcodeType.Texld] = new("texld"),
		[OpcodeType.Texbem] = new("texbem"),
		[OpcodeType.Texbeml] = new("texbeml"),
		[OpcodeType.Texreg2ar] = new("texreg2ar"),
		[OpcodeType.Texreg2gb] = new("texreg2gb"),
		[OpcodeType.Texm3x2pad] = new("texm3x2pad"),
		[OpcodeType.Texm3x2tex] = new("texm3x2tex"),
		[OpcodeType.Texm3x3pad] = new("texm3x3pad"),
		[OpcodeType.Texm3x3tex] = new("texm3x3tex"),
		[OpcodeType.Texm3x3spec] = new("texm3x3spec"),
		[OpcodeType.Texm3x3vspec] = new("texm3x3vspec"),
		[OpcodeType.Expp] = new("expp"),
		[OpcodeType.Logp] = new("logp"),
		[OpcodeType.Cnd] = new("cnd"),
		[OpcodeType.Def] = new("def"),
		[OpcodeType.Texreg2rgb] = new("texreg2rgb"),
		[OpcodeType.Texdp3tex] = new("texdp3tex"),
		[OpcodeType.Texm3x2depth] = new("texm3x2depth"),
		[OpcodeType.Texdp3] = new("texdp3"),
		[OpcodeType.Texm3x3] = new("texm3x3"),
		[OpcodeType.Texdepth] = new("texdepth"),
		[OpcodeType.Cmp] = new("cmp"),
		[OpcodeType.Bem] = new("bem"),
		[OpcodeType.Dp2add] = new("dp2add"),
		[OpcodeType.Dsx] = new("dsx"),
		[OpcodeType.Dsy] = new("dsy"),
		[OpcodeType.Texldd] = new("texldd"),
		[OpcodeType.Setp] = new("setp"),
		[OpcodeType.Texldl] = new("texldl"),
		[OpcodeType.Breakp] = new("breakp"),
		[OpcodeType.Comment] = new("comment"),
		[OpcodeType.End] = new("end")
	};

	public OpcodeType Type = OpcodeType.Nop;
	public uint Length = 0;

	public byte[]? Comment = null;

	public DestinationParameter? Destination = null;
	public SourceParameter[] Sources = Array.Empty<SourceParameter>();

	public float[]? Constant = null;
	public uint? Extra = null;

	public Opcode() {
	}

	public override string ToString() {
		if (Type == OpcodeType.Comment)
			return Comment is null ? "" : "// " + Encoding.ASCII.GetString(Comment);

		StringBuilder sb = new();
		if (OpcodeInfo.TryGetValue(Type, out OpcodeTypeInfo info))
			sb.Append(info.Name);
		else
			sb.Append("???");

		bool hadParam = false;

		if (Destination is not null) {
			sb.Append(' ');
			sb.Append(Destination.ToString());
			hadParam = true;
		}
		if (Constant is not null) {
			if (hadParam)
				sb.Append(", ");
			else
				sb.Append(' ');

			for (int i = 0; i < Constant.Length; i++) {
				if (i > 0)
					sb.Append(", ");
				sb.Append(Constant[i].ToString(CultureInfo.InvariantCulture));
			}
		}
		foreach (SourceParameter src in Sources) {
			if (hadParam)
				sb.Append(", ");
			else
				sb.Append(' ');
			sb.Append(src.ToString());
			hadParam = true;
		}

		return sb.ToString();
	}
}

public record struct OpcodeTypeInfo(string Name, bool NoDest = false);
