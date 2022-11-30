using ShaderDecompiler.Structures;
using System.Data;
using System.Text;

namespace ShaderDecompiler
{
    public class Shader
    {
        static readonly uint CTABHeader = 'C' | 'T' << 8 | 'A' << 16 | 'B' << 24;

        public ShaderVersion Version { get; private set; }

        public List<Opcode> Opcodes { get; } = new();
        public List<Constant> Constants { get; } = new();

        public Dictionary<uint, TypeInfo> Types { get; } = new();

        public string? Creator { get; private set; }
        public string? Target { get; private set; }

        public static Shader Read(BinaryReader reader)
        {
            Shader shader = new Shader();

            shader.Version = ShaderVersion.Read(reader);

            while (shader.ReadOpcode(reader)) { }

            return shader;
        }

        bool ReadOpcode(BinaryReader reader)
        {
            Opcode op = new();
            BitNumber token = new(reader.ReadUInt32());

            op.Type = (OpcodeType)token[0..15];
            bool skipAdd = false;

            if (op.Type == OpcodeType.Comment)
            {
                op.Length = token[16..30];
                long commentStart = reader.BaseStream.Position;
                if (!ProcessSpecialComments(reader, op.Length * 4))
                {
                    reader.BaseStream.Seek(commentStart, SeekOrigin.Begin);
                    op.Comment = reader.ReadBytes((int)op.Length * 4);
                }
                else
                    reader.BaseStream.Seek(commentStart + op.Length * 4, SeekOrigin.Begin);
            }

            else if (op.Type == OpcodeType.End)
            {
                skipAdd = true;
            }
            else
            {
                op.Length = token[24..27];

                if (op.Length > 0)
                {
                    if (op.Type == OpcodeType.Call || op.Type == OpcodeType.Callnz)
                    {
                        // read label param
                        throw new NotImplementedException();
                    }
                    else if (op.Type == OpcodeType.Def)
                    {
                        op.Destination = DestinationParameter.Read(reader);
                        op.Constant = new float[] { reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle() };
                    }
                    else if (op.Type == OpcodeType.Dcl)
                    {
                        op.Extra = reader.ReadUInt32();
                        op.Destination = DestinationParameter.Read(reader);
                    }
                    else
                    {
                        if (op.Length >= 1) op.Destination = DestinationParameter.Read(reader);
                        if (op.Length >= 2)
                        {
                            op.Sources = new SourceParameter[op.Length - 1];
                            for (int i = 0; i < op.Sources.Length; i++)
                                op.Sources[i] = SourceParameter.Read(reader);
                        }
                    }
                }
            }

            if (!skipAdd)
                Opcodes.Add(op);
            return op.Type != OpcodeType.End;
        }

        bool ProcessSpecialComments(BinaryReader reader, uint length)
        {
            uint header = reader.ReadUInt32();
            if (header == CTABHeader)
            {
                ReadCTAB(reader, length - 4);
                return true;
            }
            return false;
        }

        void ReadCTAB(BinaryReader reader, uint length)
        {
            uint start = (uint)reader.BaseStream.Position;

            if (reader.ReadUInt32() != 28)
                Warning("Wrong CTAB length");

            Creator = ReadString(reader, start, length);
            ShaderVersion version = ShaderVersion.Read(reader);
            if (version != Version)
                Warning("Wrong CTAB shader version");

            uint constantsNum = reader.ReadUInt32();
            uint constantsInfo = reader.ReadUInt32();
            reader.ReadUInt32();

            Target = ReadString(reader, start, length);

            for (int i = 0; i < constantsNum; i++)
            {
                reader.BaseStream.Seek(start + constantsInfo + i * 20, SeekOrigin.Begin);

                Constant constant = new Constant();
                constant.Name = ReadString(reader, start, length);
                constant.RegSet = (RegSet)reader.ReadUInt16();
                constant.RegIndex = reader.ReadUInt16();
                constant.RegCount = reader.ReadUInt16();
                reader.ReadUInt16();
                constant.TypeInfo = ReadTypeInfo(reader, start, length);
                uint defValue = reader.ReadUInt32();
                if (defValue > 0)
                {
                    reader.BaseStream.Seek(start + defValue, SeekOrigin.Begin);
                    uint typeSize = constant.TypeInfo.GetSize(out uint typeActualSize);

                    constant.DefaultValue = new float[typeActualSize];
                    for (uint j = 0; j < typeSize; j++)
                    {
                        float value = reader.ReadSingle();

                        if (constant.TypeInfo.TransformTypeDefaultValueDataPos(j, out uint arrayPos))
                            constant.DefaultValue[arrayPos] = value;
                    }
                }

                Constants.Add(constant);
            }

            Constants.Sort((a, b) => a.RegIndex - b.RegIndex);
        }

        void Warning(string warning)
        {
            Console.Write("Warning: ");
            Console.WriteLine(warning);
        }

        TypeInfo ReadTypeInfo(BinaryReader reader, uint start, uint length)
        {
            uint position = reader.ReadUInt32();

            if (Types.TryGetValue(position, out TypeInfo? cachedType))
                return cachedType;


            long streamPos = reader.BaseStream.Position;

            try
            {
                reader.BaseStream.Seek(start + position, SeekOrigin.Begin);

                TypeInfo info = new();

                info.Class = (ObjectClass)reader.ReadUInt16();
                info.Type = (ObjectType)reader.ReadUInt16();
                info.Rows = reader.ReadUInt16();
                info.Columns = reader.ReadUInt16();
                info.Elements = reader.ReadUInt16();

                ushort memberCount = reader.ReadUInt16();

                if (memberCount > 0)
                {
                    info.StructMembers = new Value[memberCount];

                    uint membersPos = reader.ReadUInt32();
                    reader.BaseStream.Seek(start + membersPos, SeekOrigin.Begin);

                    for (int i = 0; i < memberCount; i++)
                    {
                        Value member = new();

                        member.Name = ReadString(reader, start, length);
                        member.Type = ReadTypeInfo(reader, start, length);
                        info.StructMembers[i] = member;
                    }
                }

                Types[position] = info;

                return info;
            }
            finally
            {
                reader.BaseStream.Seek(streamPos, SeekOrigin.Begin);
            }

        }

        public override string ToString()
        {
            return
                $"// {Version}\n" +
                $"// {Creator}\n" +
                $"// {Target}\n" +
                $"\n" +
                $"{string.Join('\n', Constants)}\n" +
                $"\n" +
                $"{string.Join('\n', Opcodes.Where(o => o.Type != OpcodeType.Comment || o.Comment is not null))}";
        }

        static string? ReadString(BinaryReader reader, uint start, uint length)
        {
            uint position = reader.ReadUInt32();

            long streamPos = reader.BaseStream.Position;
            position += start;

            if (position >= start + length)
                return null;

            try
            {
                reader.BaseStream.Seek(position, SeekOrigin.Begin);

                int strLength = 0;
                while (position + strLength < start + length)
                {
                    if (reader.ReadByte() == 0)
                    {
                        reader.BaseStream.Seek(position, SeekOrigin.Begin);
                        return Encoding.ASCII.GetString(reader.ReadBytes(strLength));
                    }
                    else if (reader.BaseStream.Position >= reader.BaseStream.Length)
                        return null;

                    strLength++;
                }
                return null;
            }
            finally
            {
                reader.BaseStream.Seek(streamPos, SeekOrigin.Begin);
            }
        }

    }
}
