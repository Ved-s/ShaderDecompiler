namespace ShaderDecompiler.Structures;

public class Constant
{
    public string? Name;
    public RegSet RegSet;
    public ushort RegIndex;
    public ushort RegCount;
    public TypeInfo TypeInfo = null!;
    public float[]? DefaultValue;

    public override string? ToString()
    {
        string size = RegCount == 1 ? "" : $" // Size: {RegCount}";
        string defaultValue = "";
        if (DefaultValue is not null)
        {
            if (DefaultValue.Length == 1) defaultValue = " = " + GetTypeDefaultValue(TypeInfo, DefaultValue[0], 0);
            else defaultValue = $" = {{ {string.Join(", ", DefaultValue.Select((v, n) => GetTypeDefaultValue(TypeInfo, v, (uint)n)))} }}";
        }

        return $"{TypeInfo} {Name} : register(C{RegIndex}){defaultValue};{size}";
    }

    string GetTypeDefaultValue(TypeInfo type, float value, uint pos)
    {
        if (pos >= 0 && type.Class == ObjectClass.Struct)
        {
            uint size = 0;

            foreach (var member in type.StructMembers)
            {
                member.Type.GetSize(out uint memberActualSize);

                if (size + memberActualSize > pos)
                    return GetTypeDefaultValue(member.Type, value, pos - size);


                size += memberActualSize;
            }
        }

        if (type.Type == ObjectType.Bool)
            return value == 0 ? "false" : "true";
        if (type.Type == ObjectType.Int)
            return ((int)value).ToString();
        return value.ToString();
    }
}


