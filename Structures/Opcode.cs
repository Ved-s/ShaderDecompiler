using System.Globalization;
using System.Numerics;
using System.Text;

namespace ShaderDecompiler.Structures;

public struct Opcode
{
    public static readonly Dictionary<OpcodeType, string> OpcodeNames = new() { [OpcodeType.Nop] = "nop", [OpcodeType.Mov] = "mov", [OpcodeType.Add] = "add", [OpcodeType.Sub] = "sub", [OpcodeType.Mad] = "mad", [OpcodeType.Mul] = "mul", [OpcodeType.Rcp] = "rcp", [OpcodeType.Rsq] = "rsq", [OpcodeType.Dp3] = "dp3", [OpcodeType.Dp4] = "dp4", [OpcodeType.Min] = "min", [OpcodeType.Max] = "max", [OpcodeType.Slt] = "slt", [OpcodeType.Sge] = "sge", [OpcodeType.Exp] = "exp", [OpcodeType.Log] = "log", [OpcodeType.Lit] = "lit", [OpcodeType.Dst] = "dst", [OpcodeType.Lrp] = "lrp", [OpcodeType.Frc] = "frc", [OpcodeType.M4x4] = "m4x4", [OpcodeType.M4x3] = "m4x3", [OpcodeType.M3x4] = "m3x4", [OpcodeType.M3x3] = "m3x3", [OpcodeType.M3x2] = "m3x2", [OpcodeType.Call] = "call", [OpcodeType.Callnz] = "callnz", [OpcodeType.Loop] = "loop", [OpcodeType.Ret] = "ret", [OpcodeType.Endloop] = "endloop", [OpcodeType.Label] = "label", [OpcodeType.Dcl] = "dcl", [OpcodeType.Pow] = "pow", [OpcodeType.Crs] = "crs", [OpcodeType.Sgn] = "sgn", [OpcodeType.Abs] = "abs", [OpcodeType.Nrm] = "nrm", [OpcodeType.Sincos] = "sincos", [OpcodeType.Rep] = "rep", [OpcodeType.Endrep] = "endrep", [OpcodeType.If] = "if", [OpcodeType.Ifc] = "ifc", [OpcodeType.Else] = "else", [OpcodeType.Endif] = "endif", [OpcodeType.Break] = "break", [OpcodeType.Breakc] = "breakc", [OpcodeType.Mova] = "mova", [OpcodeType.Defb] = "defb", [OpcodeType.Defi] = "defi", [OpcodeType.Texcrd] = "texcrd", [OpcodeType.Texkill] = "texkill", [OpcodeType.Texld] = "texld", [OpcodeType.Texbem] = "texbem", [OpcodeType.Texbeml] = "texbeml", [OpcodeType.Texreg2ar] = "texreg2ar", [OpcodeType.Texreg2gb] = "texreg2gb", [OpcodeType.Texm3x2pad] = "texm3x2pad", [OpcodeType.Texm3x2tex] = "texm3x2tex", [OpcodeType.Texm3x3pad] = "texm3x3pad", [OpcodeType.Texm3x3tex] = "texm3x3tex", [OpcodeType.Texm3x3spec] = "texm3x3spec", [OpcodeType.Texm3x3vspec] = "texm3x3vspec", [OpcodeType.Expp] = "expp", [OpcodeType.Logp] = "logp", [OpcodeType.Cnd] = "cnd", [OpcodeType.Def] = "def", [OpcodeType.Texreg2rgb] = "texreg2rgb", [OpcodeType.Texdp3tex] = "texdp3tex", [OpcodeType.Texm3x2depth] = "texm3x2depth", [OpcodeType.Texdp3] = "texdp3", [OpcodeType.Texm3x3] = "texm3x3", [OpcodeType.Texdepth] = "texdepth", [OpcodeType.Cmp] = "cmp", [OpcodeType.Bem] = "bem", [OpcodeType.Dp2add] = "dp2add", [OpcodeType.Dsx] = "dsx", [OpcodeType.Dsy] = "dsy", [OpcodeType.Texldd] = "texldd", [OpcodeType.Setp] = "setp", [OpcodeType.Texldl] = "texldl", [OpcodeType.Breakp] = "breakp", [OpcodeType.Comment] = "comment", [OpcodeType.End] = "end" };

    public OpcodeType Type = OpcodeType.Nop;
    public uint Length = 0;

    public byte[]? Comment = null;

    public DestinationParameter? Destination = null;
    public SourceParameter[] Sources = Array.Empty<SourceParameter>();

    public float[]? Constant = null;
    public uint? Extra = null;

    public Opcode()
    {
    }

    public override string ToString()
    {
        if (Type == OpcodeType.Comment)
            return Comment is null ? "" : "// " + Encoding.ASCII.GetString(Comment);

        StringBuilder sb = new();
        if (OpcodeNames.TryGetValue(Type, out string? name))
            sb.Append(name);
        else
            sb.Append("???");

        bool hadParam = false;

        if (Destination.HasValue)
        {
            sb.Append(' ');
            sb.Append(Destination.ToString());
            hadParam = true;
        }
        if (Constant is not null)
        {
            if (hadParam)
                sb.Append(", ");
            else
                sb.Append(' ');

            for (int i = 0; i < Constant.Length; i++)
            {
                if (i > 0)
                    sb.Append(", ");
                sb.Append(Constant[i].ToString(CultureInfo.InvariantCulture));
            }
        }
        foreach (SourceParameter? src in Sources)
        {
            if (!src.HasValue)
                continue;

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

