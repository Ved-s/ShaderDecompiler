using ShaderDecompiler.Structures;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ShaderDecompiler
{
    public class HLSLEffect
    {
        private long BasePosition;
        private BinaryReader Reader = null!; // not null when reading

        public Parameter[] Parameters = Array.Empty<Parameter>();
        public Technique[] Techniques = Array.Empty<Technique>();
        public EffectObject[] Objects = Array.Empty<EffectObject>();

        public static HLSLEffect Read(BinaryReader reader)
        {
            uint magic = reader.ReadUInt32();
            if (magic == 0xbcf00bcf)
            {
                uint skip = reader.ReadUInt32() - 8;
                reader.BaseStream.Seek(skip, SeekOrigin.Current);

                magic = reader.ReadUInt32();
            }

            if (magic == 0xfeff0901) // HLSL Effect
            {
                HLSLEffect effect = new();
                effect.LoadHLSL(reader);
                return effect;
            }

            uint type = (magic & 0xffff0000) >> 16;
            reader.BaseStream.Seek(-4, SeekOrigin.Current);

            if (type == 0xffff) // only PixelShader
            {
                throw new NotImplementedException();
            }
            else if (type == 0xfffe) // only VertexShader
            {
                throw new NotImplementedException();
            }
            else throw new InvalidDataException();
        }

        void LoadHLSL(BinaryReader reader)
        {
            Reader = reader;
            uint offset = reader.ReadUInt32();

            BasePosition = reader.BaseStream.Position;

            reader.BaseStream.Seek(offset, SeekOrigin.Current);

            uint numparams = reader.ReadUInt32();
            uint numtechniques = reader.ReadUInt32();
            reader.ReadUInt32();
            uint numobjects = reader.ReadUInt32();

            if (numobjects > 0)
                Objects = new EffectObject[numobjects];

            ReadParameters(numparams);
            ReadTechniques(numtechniques);

            uint numsmallobj = reader.ReadUInt32();
            uint numlargeobj = reader.ReadUInt32();

            ReadSmallObjects(numsmallobj);
            ReadLargeObjects(numlargeobj);

            Reader = null!;
        }

        void ReadParameters(uint count)
        {
            if (count == 0)
                return;

            Parameters = new Parameter[count];
            for (int i = 0; i < count; i++)
            {
                Parameter p = new();
                Parameters[i] = p;

                uint typeptr = Reader.ReadUInt32();
                uint valueptr = Reader.ReadUInt32();
                p.Flags = Reader.ReadUInt32();
                uint numannotations = Reader.ReadUInt32();

                ReadAnnotations(numannotations, p);
                p.Value = ReadValue(typeptr, valueptr);
                
            }
        }
        void ReadTechniques(uint count)
        {
            if (count == 0)
                return;

            Techniques = new Technique[count];
            
            for (int t = 0; t < count; t++)
            {
                Technique tech = new();
                Techniques[t] = tech;

                tech.Name = ReadString();
                uint numannotations = Reader.ReadUInt32();
                uint numpasses = Reader.ReadUInt32();

                ReadAnnotations(numannotations, tech);

                if (numpasses == 0)
                    continue;

                tech.Passes = new Pass[numpasses];

                for (int p = 0; p < numpasses; p++)
                {
                    Pass pass = new();
                    tech.Passes[p] = pass;

                    pass.Name = ReadString();
                    numannotations = Reader.ReadUInt32();
                    uint numstates = Reader.ReadUInt32();

                    ReadAnnotations(numannotations, pass);

                    if (numstates == 0)
                        continue;

                    pass.States = new State[numstates];
                    for (int s = 0; s < numstates; s++)
                    {
                        State state = new();
                        pass.States[s] = state;

                        state.Type = (StateType)Reader.ReadUInt32();

                        Reader.ReadUInt32();
                        uint typeptr = Reader.ReadUInt32();
                        uint valueptr = Reader.ReadUInt32();

                        state.Value = ReadValue(typeptr, valueptr);
                    }
                }
            }
        }

        void ReadSmallObjects(uint count)
        {
            if (count == 0)
                return;

            for (int i = 0; i < count; i++)
            {
                uint index = Reader.ReadUInt32();
                uint length = Reader.ReadUInt32();

                EffectObject obj = Objects[index];

                if (obj.Type == ObjectType.String)
                {
                    if (length > 0)
                        obj.Object = ReadStringHere(length);
                    Reader.ReadByte();
                }
                else if (obj.Type >= ObjectType.Texture && obj.Type <= ObjectType.Samplercube)
                {
                    if (length > 0)
                        obj.Object = ReadStringHere(length);
                }
                else if (obj.Type == ObjectType.PixelShader || obj.Type == ObjectType.VertexShader)
                {
                    Debugger.Break();
                }
                else 
                {
                    obj.Object = Reader.ReadBytes((int)length);
                }

                Reader.BaseStream.Seek((4 - length % 4) % 4, SeekOrigin.Current);
            }
        }
        void ReadLargeObjects(uint count)
        {
            if (count == 0)
                return;

            for (int i = 0; i < count; i++)
            {
                uint technique = Reader.ReadUInt32();
                uint index = Reader.ReadUInt32();
                uint something = Reader.ReadUInt32();
                uint state = Reader.ReadUInt32();
                uint type = Reader.ReadUInt32();
                uint length = Reader.ReadUInt32();

                uint objIndex = technique > Techniques.Length ?
                    ((Parameters[index].Value.Object as SamplerState[])![state].Value.Object as uint[])![0] :
                    (Techniques[technique].Passes[index].States[state].Value.Object as uint[])![0];

                EffectObject obj = Objects[objIndex];

                if (obj.Type == ObjectType.String)
                {
                    if (length > 0)
                        obj.Object = ReadStringHere(length);
                }
                else if (obj.Type >= ObjectType.Texture && obj.Type <= ObjectType.Samplercube)
                {
                    if (length > 0)
                        obj.Object = ReadStringHere(length);
                }
                else if (obj.Type == ObjectType.PixelShader || obj.Type == ObjectType.VertexShader)
                {
                    obj.Object = Shader.Read(Reader);
                }
                else
                {
                    obj.Object = Reader.ReadBytes((int)length);
                }

                Reader.BaseStream.Seek((4 - length % 4) % 4, SeekOrigin.Current);
            }
        }

        void ReadAnnotations(uint count, AnnotatedObject @object)
        {
            if (count == 0)
                return;

            @object.Annotations = new Value[count];
            for (int i = 0; i < count; i++)
            {
                uint typeptr = Reader.ReadUInt32();
                uint valueptr = Reader.ReadUInt32();

                @object.Annotations[i] = ReadValue(typeptr, valueptr);
            }
        }
        
        Value ReadValue(uint typeptr, uint valueptr)
        {
            long readerpos = Reader.BaseStream.Position;
            try
            {
                Value value = new();

                Reader.BaseStream.Seek(BasePosition + typeptr, SeekOrigin.Begin);
                ReadValueInfo(value);

                Reader.BaseStream.Seek(BasePosition + valueptr, SeekOrigin.Begin);
                ReadValueData(value);

                return value;
            }
            finally
            {
                Reader.BaseStream.Seek(readerpos, SeekOrigin.Begin);
            }
        }
        void ReadValueInfo(Value value)
        {
            value.Type.Type = (ObjectType)Reader.ReadUInt32();
            value.Type.Class = (ObjectClass)Reader.ReadUInt32();
            value.Name = ReadString();
            value.Semantic = ReadString();
            value.Type.Elements = Reader.ReadUInt32();

            if (value.Type.Class >= ObjectClass.Scalar && value.Type.Class <= ObjectClass.MatrixColumns)
            {
                value.Type.Columns = Reader.ReadUInt32();
                value.Type.Rows = Reader.ReadUInt32();
            }
            else if (value.Type.Class == ObjectClass.Struct)
            {
                uint members = Reader.ReadUInt32();
                List<Value> memberList = new();
                for (int i = 0; i < members; i++)
                {
                    Value m = new();
                    ReadValueInfo(m);
                    if (m.Type.Class == ObjectClass.Struct)
                        members--;

                    memberList.Add(m);
                }
                value.Type.StructMembers = memberList.ToArray();
            }
        }
        void ReadValueData(Value value)
        {
            if (value.Type.Class >= ObjectClass.Scalar && value.Type.Class <= ObjectClass.MatrixColumns)
            {
                uint size = value.Type.Columns * value.Type.Rows;
                if (value.Type.Elements > 0)
                    size *= value.Type.Elements;

                switch (value.Type.Type)
                {
                    case ObjectType.Int:
                        int[] ints = new int[size];
                        for (int i = 0; i < size; i++)
                            ints[i] = Reader.ReadInt32();
                        value.Object = ints;
                        break;

                    case ObjectType.Float:
                        float[] floats = new float[size];
                        for (int i = 0; i < size; i++)
                            floats[i] = Reader.ReadSingle();
                        value.Object = floats;
                        break;

                    case ObjectType.Bool:
                        bool[] bools = new bool[size];
                        for (int i = 0; i < size; i++)
                            bools[i] = Reader.ReadBoolean();
                        value.Object = bools;
                        break;
                        
                    default:
                        Debugger.Break();
                        break;
                }

                
            }
            else if (value.Type.Class == ObjectClass.Object)
            {
                if (value.Type.Type >= ObjectType.Sampler && value.Type.Type <= ObjectType.Samplercube)
                {
                    uint numstates = Reader.ReadUInt32();

                    SamplerState[] states = new SamplerState[numstates];

                    for (int i = 0; i < numstates; i++)
                    {
                        SamplerState state = new();
                        states[i] = state;

                        state.Type = (SamplerStateType)(Reader.ReadUInt32() & ~0xA0);
                        uint something = Reader.ReadUInt32();

                        uint statetypeptr = Reader.ReadUInt32();
                        uint statevalueptr = Reader.ReadUInt32();
                        state.Value = ReadValue(statetypeptr, statevalueptr);

                        if (state.Type == SamplerStateType.Texture && state.Value.Object is uint[] idarray)
                        {
                            Objects[idarray[0]] = new EffectObject
                            {
                                Type = value.Type.Type,
                            };
                        }
                    }
                    value.Object = states;
                }
                else
                {
                    uint count = Math.Max(value.Type.Elements, 1);
                    uint[] ids = new uint[count];

                    for (int i = 0; i < count; i++)
                    {
                        ids[i] = Reader.ReadUInt32();
                        Objects[ids[i]] = new EffectObject
                        {
                            Type = value.Type.Type,
                        };
                    }

                    value.Object = ids;
                }
            }
            else if (value.Type.Class == ObjectClass.Struct)
            {
                for (int i = 0; i < value.Type.StructMembers.Length; i++)
                    ReadValueData(value.Type.StructMembers[i]);
            }
        }

        string? ReadString()
        {
            uint ptr = Reader.ReadUInt32();
            if (ptr == 0 || BasePosition + ptr >= Reader.BaseStream.Length)
                return null;

            long readerpos = Reader.BaseStream.Position;
            try
            {
                Reader.BaseStream.Seek(BasePosition + ptr, SeekOrigin.Begin);

                uint len = Reader.ReadUInt32();
                return ReadStringHere(len);
            }
            finally 
            {
                Reader.BaseStream.Seek(readerpos, SeekOrigin.Begin);
            }
        }

        string? ReadStringHere(uint length)
        {
            if (length == 0)
                return null;

            return Encoding.ASCII.GetString(Reader.ReadBytes((int)length - 1));
        }

    }
}
