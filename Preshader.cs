#region License
/*
  ShaderDecompiler - Direct3D shader decompiler

  Released under Microsoft Public License
  See LICENSE for details
*/
#endregion

using ShaderDecompiler.Structures;
using System.Diagnostics;

namespace ShaderDecompiler {
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
					return true;

				case PRSIHeader:
					ReadPRSI(reader);
					return true;

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

				uint actualRegister = operandItem / 4;
				Swizzle swizzle = (Swizzle)(operandItem % 4);

				if (operandType == 1) {
					swizzle = Swizzle.X;
					actualRegister = operandItem;
				}

				OpcodeParameter param;
				if (i == opcode.Length - 1) {
					opcode.Destination = new() { 
						WriteX = swizzle == Swizzle.X,
						WriteY = swizzle == Swizzle.Y,
						WriteZ = swizzle == Swizzle.Z,
						WriteW = swizzle == Swizzle.W,
					};
					param = opcode.Destination;
				}
				else {
					opcode.Sources[i] = new() {
						SwizzleX = swizzle,
						SwizzleY = swizzle,
						SwizzleZ = swizzle,
						SwizzleW = swizzle
					};
					param = opcode.Sources[i];
				}
				param.Register = actualRegister;

				switch (operandType) {
					case 1:
						param.RegisterType = ParameterRegisterType.PreshaderLiteral;
						break;

					case 2:
						param.RegisterType = ParameterRegisterType.PreshaderInput;
						for (int j = 0; j < operandArrayCount; j++) {
							reader.ReadUInt32();
							reader.ReadUInt32();
						}

						break;

					case 4:
						param.RegisterType = ParameterRegisterType.Const;
						break;

					case 7:
						param.RegisterType = ParameterRegisterType.PreshaderTemp;
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
