using ShaderDecompiler.Structures;
using System.Data;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Reflection.PortableExecutable;
using System.Text;

namespace ShaderDecompiler {
	public class Shader {
		public const uint CTABHeader = 'C' | 'T' << 8 | 'A' << 16 | 'B' << 24;
		public const uint PRESHeader = 'P' | 'R' << 8 | 'E' << 16 | 'S' << 24;

		public ShaderVersion Version { get; set; }

		public List<Opcode> Opcodes { get; } = new();
		public List<Constant> Constants { get; } = new();

		public Dictionary<uint, TypeInfo> Types { get; } = new();

		public Preshader? Preshader = null;

		public string? Creator { get; private set; }
		public string? Target { get; private set; }

		public static Shader Read(BinaryReader reader) {
			Shader shader = new() {
				Version = ShaderVersion.Read(reader)
			};

			while (shader.ReadOpcode(reader)) { }

			return shader;
		}

		protected bool ReadOpcode(BinaryReader reader) {
			Opcode op = new();
			BitNumber token = new(reader.ReadUInt32());

			op.Type = (OpcodeType)token[0..15];
			bool skipAdd = false;

			if (op.Type == OpcodeType.Comment) {
				op.Length = token[16..30];
				long commentStart = reader.BaseStream.Position;
				if (!ProcessSpecialComments(reader, op.Length * 4)) {
					reader.BaseStream.Seek(commentStart, SeekOrigin.Begin);
					op.Comment = reader.ReadBytes((int)op.Length * 4);
				}
				else {
					reader.BaseStream.Seek(commentStart + op.Length * 4, SeekOrigin.Begin);
					skipAdd = true;
				}
			}

			else if (op.Type == OpcodeType.End) {
				skipAdd = true;
			}
			else {
				op.Length = token[24..27];

				if (op.Length > 0) {
					if (op.Type == OpcodeType.Call || op.Type == OpcodeType.Callnz) {
						// read label param
						throw new NotImplementedException();
					}
					else if (op.Type == OpcodeType.Def) {
						op.Destination = DestinationParameter.Read(reader, Version);
						op.Constant = new float[] { reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle() };
					}
					else if (op.Type == OpcodeType.Dcl) {
						op.Extra = reader.ReadUInt32();
						op.Destination = DestinationParameter.Read(reader, Version);
					}
					else {
						int i = 0;

						if (op.Length > i && !Opcode.OpcodeInfo[op.Type].NoDest) {
							op.Destination = DestinationParameter.Read(reader, Version);
							i++;
						}

						if (op.Length > i) {
							int start = i;
							op.Sources = new SourceParameter[op.Length - i];
							for (; i < op.Length; i++)
								op.Sources[i - start] = SourceParameter.Read(reader, Version);
						}
					}
				}
			}

			if (!skipAdd)
				Opcodes.Add(op);
			return op.Type != OpcodeType.End;
		}

		protected virtual bool ProcessSpecialComments(BinaryReader reader, uint length) {
			uint header = reader.ReadUInt32();
			switch (header) {
				case CTABHeader:
					ReadConstantTable(reader, length - 4);
					return true;

				case PRESHeader:
					Preshader = Preshader.Read(reader, this);

					foreach (Constant @const in Preshader.Constants)
						if (!Constants.Any(c => c.RegSet == @const.RegSet && c.RegIndex == @const.RegIndex))
							Constants.Add(@const);

					return true;

			}
			return false;
		}

		protected void ReadConstantTable(BinaryReader reader, uint length) {
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

			for (int i = 0; i < constantsNum; i++) {
				reader.BaseStream.Seek(start + constantsInfo + i * 20, SeekOrigin.Begin);

				Constant constant = new() {
					Name = ReadString(reader, start, length),
					RegSet = (RegSet)reader.ReadUInt16(),
					RegIndex = reader.ReadUInt16(),
					RegCount = reader.ReadUInt16()
				};
				reader.ReadUInt16();
				constant.TypeInfo = ReadTypeInfo(reader, start, length);
				uint defValue = reader.ReadUInt32();
				if (defValue > 0) {
					reader.BaseStream.Seek(start + defValue, SeekOrigin.Begin);
					uint typeSize = constant.TypeInfo.GetSize(out uint typeActualSize);

					constant.DefaultValue = new float[typeActualSize];
					for (uint j = 0; j < typeSize; j++) {
						float value = reader.ReadSingle();

						if (constant.TypeInfo.TransformTypeDefaultValueDataPos(j, out uint arrayPos))
							constant.DefaultValue[arrayPos] = value;
					}
				}

				Constants.Add(constant);
			}

			Constants.Sort((a, b) => a.RegIndex - b.RegIndex);
		}

		void Warning(string warning) {
			Console.Write("Warning: ");
			Console.WriteLine(warning);
		}

		TypeInfo ReadTypeInfo(BinaryReader reader, uint start, uint length) {
			uint position = reader.ReadUInt32();

			if (Types.TryGetValue(position, out TypeInfo? cachedType))
				return cachedType;

			long streamPos = reader.BaseStream.Position;

			try {
				reader.BaseStream.Seek(start + position, SeekOrigin.Begin);

				TypeInfo info = new() {
					Class = (ObjectClass)reader.ReadUInt16(),
					Type = (ObjectType)reader.ReadUInt16(),
					Rows = reader.ReadUInt16(),
					Columns = reader.ReadUInt16(),
					Elements = reader.ReadUInt16()
				};

				ushort memberCount = reader.ReadUInt16();

				if (memberCount > 0) {
					info.StructMembers = new Value[memberCount];

					uint membersPos = reader.ReadUInt32();
					reader.BaseStream.Seek(start + membersPos, SeekOrigin.Begin);

					for (int i = 0; i < memberCount; i++) {
						Value member = new() {
							Name = ReadString(reader, start, length),
							Type = ReadTypeInfo(reader, start, length)
						};
						info.StructMembers[i] = member;
					}
				}

				Types[position] = info;

				return info;
			}
			finally {
				reader.BaseStream.Seek(streamPos, SeekOrigin.Begin);
			}

		}

		public override string ToString() {
			return
				$"// {Version}\n" +
				$"// {Creator}\n" +
				$"// {Target}\n" +
				$"\n" +
				$"{string.Join('\n', Constants)}\n" +
				$"\n" +
				$"{string.Join('\n', Opcodes.Where(o => o.Type != OpcodeType.Comment || o.Comment is not null))}";
		}

		static string? ReadString(BinaryReader reader, uint start, uint length) {
			uint position = reader.ReadUInt32();

			long streamPos = reader.BaseStream.Position;
			position += start;

			if (position >= start + length)
				return null;

			try {
				reader.BaseStream.Seek(position, SeekOrigin.Begin);

				int strLength = 0;
				while (position + strLength < start + length) {
					if (reader.ReadByte() == 0) {
						reader.BaseStream.Seek(position, SeekOrigin.Begin);
						return Encoding.ASCII.GetString(reader.ReadBytes(strLength));
					}
					else if (reader.BaseStream.Position >= reader.BaseStream.Length)
						return null;

					strLength++;
				}
				return null;
			}
			finally {
				reader.BaseStream.Seek(streamPos, SeekOrigin.Begin);
			}
		}
	}

	public class Preshader : Shader {

		public const uint FXLCHeader = 'F' | 'X' << 8 | 'L' << 16 | 'C' << 24;
		public const uint CLITHeader = 'C' | 'L' << 8 | 'I' << 16 | 'T' << 24;
		public const uint PRSIHeader = 'P' | 'R' << 8 | 'S' << 16 | 'I' << 24;

		public readonly Shader Shader;

		public double[]? Literals;

		public Preshader(Shader shader) {
			Shader = shader;
		}

		public static Preshader Read(BinaryReader reader, Shader shader) {
			Preshader preshader = new(shader) {
				Version = ShaderVersion.Read(reader)
			};

			while (preshader.ReadOpcode(reader)) { }
			return preshader;
		}

		protected override bool ProcessSpecialComments(BinaryReader reader, uint length) {
			uint header = reader.ReadUInt32();
			switch (header) {
				case CTABHeader:
					ReadConstantTable(reader, length - 4);
					return true;

				case CLITHeader:
					ReadCLIT(reader);
					return true;

				case FXLCHeader:
					ReadFXLC(reader);
					break;

			}
			return false;
		}

		void ReadCLIT(BinaryReader reader) {
			uint count = reader.ReadUInt32();
			Literals = new double[count];
			for (int i = 0; i < count; i++) {
				Literals[i] = reader.ReadDouble();
			}
		}

		void ReadFXLC(BinaryReader reader) {
			uint opcodeCount = reader.ReadUInt32();

			for (int i = 0; i < opcodeCount; i++)
				ReadPreshaderOpcode(reader);
		}

		void ReadPRSI(BinaryReader reader) {
		}

		void ReadPreshaderOpcode(BinaryReader reader) {
			uint opToken = reader.ReadUInt32();
			OpcodeType type;

			switch ((opToken >> 16) & 0xFFFF) {
				case 0x1000: type = OpcodeType.Mov; break;
				case 0x1010: type = OpcodeType.Neg; break;
				case 0x1030: type = OpcodeType.Rcp; break;
				case 0x1040: type = OpcodeType.Frc; break;
				case 0x1050: type = OpcodeType.Exp; break;
				case 0x1060: type = OpcodeType.Log; break;
				case 0x1070: type = OpcodeType.Rsq; break;
				case 0x1080: type = OpcodeType.Sin; break;
				case 0x1090: type = OpcodeType.Cos; break;
				case 0x10A0: type = OpcodeType.Asin; break;
				case 0x10B0: type = OpcodeType.Acos; break;
				case 0x10C0: type = OpcodeType.Atan; break;
				case 0x2000: type = OpcodeType.Min; break;
				case 0x2010: type = OpcodeType.Max; break;
				case 0x2020: type = OpcodeType.Lt; break;
				case 0x2030: type = OpcodeType.Ge; break;
				case 0x2040: type = OpcodeType.Add; break;
				case 0x2050: type = OpcodeType.Mul; break;
				case 0x2060: type = OpcodeType.Atan2; break;
				case 0x2080: type = OpcodeType.Div; break;
				case 0x3000: type = OpcodeType.Cmp; break;
				case 0x3010: type = OpcodeType.Movc; break;
				case 0x5000: type = OpcodeType.Dot; break;
				case 0x5020: type = OpcodeType.Noise; break;
				case 0xA000: type = OpcodeType.MinScalar; break;
				case 0xA010: type = OpcodeType.MaxScalar; break;
				case 0xA020: type = OpcodeType.LtScalar; break;
				case 0xA030: type = OpcodeType.GeScalar; break;
				case 0xA040: type = OpcodeType.AddScalar; break;
				case 0xA050: type = OpcodeType.MulScalar; break;
				case 0xA060: type = OpcodeType.Atan2Scalar; break;
				case 0xA080: type = OpcodeType.DivScalar; break;
				case 0xD000: type = OpcodeType.DotScalar; break;
				case 0xD020: type = OpcodeType.NoiseScalar; break;
				default: type = OpcodeType.Unknown; break;
			}

			Opcode opcode = new();
			opcode.Type = type;

			uint elements = opToken & 0xff;
			opcode.Length = reader.ReadUInt32() + 1;
			opcode.Sources = new SourceParameter[opcode.Length - 1];

			for (int i = 0; i < opcode.Length; i++) {
				uint operandArrayCount = reader.ReadUInt32();
				uint operandType = reader.ReadUInt32();
				uint operandItem = reader.ReadUInt32();

				OpcodeParameter param;
				if (i == opcode.Length - 1) {
					opcode.Destination = new() { 
						WriteX = true,
						WriteY = true,
						WriteZ = true,
						WriteW = true,
					};
					param = opcode.Destination;
				}
				else {
					opcode.Sources[i] = new() {
						SwizzleX = Swizzle.X,
						SwizzleY = Swizzle.Y,
						SwizzleZ = Swizzle.Z,
						SwizzleW = Swizzle.W
					};
					param = opcode.Sources[i];
				}
				param.Register = operandItem;

				switch (operandType) {
					case 1:
						param.RegisterType = ParameterRegisterType.Literal;
						break;

					case 2:
						param.RegisterType = ParameterRegisterType.Input;
						for (int j = 0; j < operandArrayCount; j++) {
							reader.ReadUInt32();
							reader.ReadUInt32();
						}

						break;

					case 4:
						param.RegisterType = ParameterRegisterType.Output;
						break;

					case 7:
						param.RegisterType = ParameterRegisterType.Temp;
						break;

					default:
						Debugger.Break();
						break;
				}
			}

			Opcodes.Add(opcode);
		}
	}
}
