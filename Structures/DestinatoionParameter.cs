﻿using System.Text;

namespace ShaderDecompiler.Structures;

public struct DestinationParameter
{
    public uint Register;
    public ParameterRegisterType RegisterType;

    public bool WriteX;
    public bool WriteY;
    public bool WriteZ;
    public bool WriteW;

    public static DestinationParameter Read(BinaryReader reader)
    {
        DestinationParameter param = new();
        BitNumber token = new(reader.ReadUInt32());

        param.Register = token[0..10];
        param.RegisterType = (ParameterRegisterType)(token[11..12] << 3 | token[28..30]);
        param.WriteX = token[16];
        param.WriteY = token[17];
        param.WriteZ = token[18];
        param.WriteW = token[19];

        return param;
    }

    public override string ToString()
    {
        StringBuilder sb = new();

        if (SourceParameter.RegisterTypeNames.TryGetValue(RegisterType, out string? name))
            sb.Append(name);
        else
            sb.Append(RegisterType.ToString().ToLower());

        sb.Append(Register);

        if ((!WriteX || !WriteY || !WriteZ || !WriteW) && (WriteX || WriteY || WriteZ || WriteW))
        {
            sb.Append('.');
            if (WriteX) sb.Append('x');
            if (WriteY) sb.Append('y');
            if (WriteZ) sb.Append('z');
            if (WriteW) sb.Append('w');
        }

        return sb.ToString();
    }
}

