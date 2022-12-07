#region License
/*
  ShaderDecompiler - Direct3D shader decompiler

  Released under Microsoft Public License
  See LICENSE for details
*/
#endregion

using ShaderDecompiler.Decompilers.Expressions;
using ShaderDecompiler.Structures;
using System.Collections.Generic;
using System.Diagnostics;

namespace ShaderDecompiler.Decompilers {
	public class ShaderDecompiler {
		CodeWriter Writer = null!;
		Shader Shader = null!;
		ShaderDecompilationContext Context = null!;

		public ShaderDecompiler(Shader shader) {
			Shader = shader;
		}

		public void Decompile(CodeWriter writer, string entryPointName, DecompilationSettings? settings = null) {
			Writer = writer;
			Context = new(Shader, settings);

			ScanShader();
			CreateShaderRegisterNames();
			CreateExpressionList();

			Writer.WriteSpaced("void");
			Writer.WriteSpaced(entryPointName);
			Writer.Write("(");

			bool firstArg = true;
			HashSet<(DeclUsage, uint)> writtenUsages = new();
			foreach (var arg in Context.Scan.Arguments) {

				if (writtenUsages.Contains((arg.Usage, arg.UsageIndex)))
					continue;

				writtenUsages.Add((arg.Usage, arg.UsageIndex));

				Writer.LastSpace = true;
				if (!firstArg)
					Writer.Write(",");

				bool input = Context.Scan.Arguments.Any(a => a.Input && a.Usage == arg.Usage && a.UsageIndex == arg.UsageIndex);
				bool output = Context.Scan.Arguments.Any(a => a.Output && a.Usage == arg.Usage && a.UsageIndex == arg.UsageIndex);

				if (output)
					Writer.WriteSpaced(input ? "inout" : "out");

				Writer.WriteSpaced("float");
				uint argSize = arg.Size;
				if (Context.Scan.RegisterSizes.TryGetValue((arg.RegisterType, arg.Register), out uint regSize))
					argSize = regSize;

				if (argSize > 1)
					Writer.Write(argSize.ToString());
				Writer.WriteSpaced(Context.RegisterNames[(arg.RegisterType, arg.Register)]);
				Writer.WriteSpaced(":");
				Writer.WriteSpaced(arg.Usage.ToString().ToUpper());
				Writer.Write(arg.UsageIndex.ToString());

				firstArg = false;
			}
			Writer.Write(")");
			Writer.NewLine();

			Writer.StartBlock("{", "}");
			Writer.NewLine();

			for (int i = 0; i < Context.Expressions.Count; i++) {
				if (Context.Expressions[i] is null)
					continue;

				Context.CurrentExpressionIndex = i;
				Writer.Write(Context.Expressions[i]!.Decompile(Context));
				Writer.Write(";");
				Writer.NewLine();
			}

			Writer.EndBlock();
		}

		void ScanShader() {

			IEnumerable<Opcode> ops = Context.Shader.Opcodes;
			if (Context.Shader.Preshader is not null)
				ops = ops.Concat(Context.Shader.Preshader.Opcodes);

			foreach (Opcode op in ops) {
				if (op.Type == OpcodeType.Dcl && op.Extra.HasValue && op.Destination is not null) {
					BitNumber dcl = new(op.Extra.Value);
					DestinationParameter dest = op.Destination;

					ShaderArgument arg;

					switch (dest.RegisterType) {

						case ParameterRegisterType.Texture when Context.Shader.Version.CheckVersionGreaterOrEqual(ShaderType.PixelShader, 3, 0):
						case ParameterRegisterType.Input:
							arg = Context.Scan.GetArgument(dest.RegisterType, dest.Register);
							arg.Usage = (DeclUsage)dcl[0..4];
							arg.UsageIndex = dcl[16..19];
							arg.Size = dest.WriteW ? 4u : dest.WriteZ ? 3u : dest.WriteY ? 2u : 1u;
							arg.Input = true;

							if (arg.Usage == DeclUsage.Position) // PixelShaders can't have position inputs
								arg.Usage = DeclUsage.Color;
							break;

						case ParameterRegisterType.Output:
							arg = Context.Scan.GetArgument(dest.RegisterType, dest.Register);
							arg.Output = true;
							break;

						case ParameterRegisterType.Texture:
							arg = Context.Scan.GetArgument(dest.RegisterType, dest.Register);
							arg.Usage = DeclUsage.Texcoord;
							arg.UsageIndex = dest.Register;
							arg.Size = dest.WriteW ? 4u : dest.WriteZ ? 3u : dest.WriteY ? 2u : 1u;
							arg.Input = true;
							break;

						default:
							break;
					}
				}

				if (op.Destination is not null) {
					DestinationParameter dest = op.Destination;

					Context.Scan.RegistersReferenced.Add((dest.RegisterType, dest.Register, true));

					uint registerSize = dest.WriteW ? 4u : dest.WriteZ ? 3u : dest.WriteY ? 2u : 1u;

					switch (dest.RegisterType) {
						case ParameterRegisterType.Output:
						case ParameterRegisterType.Attrout:
							var arg = Context.Scan.GetArgument(dest.RegisterType, dest.Register);
							arg.Output = true;
							arg.Size = Math.Max(arg.Size, registerSize);
							break;
					}

					Context.Scan.UpdateRegisterSize(dest.RegisterType, dest.Register, registerSize);
				}

				foreach (SourceParameter? src in op.Sources) {
					if (src is null)
						continue;

					Context.Scan.RegistersReferenced.Add((src.RegisterType, src.Register, false));

					switch (src.RegisterType) {
						case ParameterRegisterType.Input:
							Context.Scan.GetArgument(src.RegisterType, src.Register).Input = true;
							break;
					}

					Swizzle maxSwizzle = (Swizzle)Math.Max(Math.Max((int)src.SwizzleX, (int)src.SwizzleY), Math.Max((int)src.SwizzleZ, (int)src.SwizzleW));
					uint registerSize = (uint)maxSwizzle + 1;

					Context.Scan.UpdateRegisterSize(src.RegisterType, src.Register, registerSize);
				}
			}

			//foreach (Constant constant in Shader.Constants) {
			//	ParameterRegisterType type = constant.RegSet switch {
			//		RegSet.Sampler => ParameterRegisterType.Sampler,
			//		_ => ParameterRegisterType.Const
			//	};
			//}

			foreach (var (type, index, dest) in Context.Scan.RegistersReferenced) {
				if (dest && type == ParameterRegisterType.Colorout) {
					ShaderArgument arg = Context.Scan.GetArgument(ParameterRegisterType.Colorout, index);
					arg.Output = true;
					arg.Usage = DeclUsage.Color;
					arg.UsageIndex = index;
				}
			}
		}

		void CreateShaderRegisterNames() {
			foreach (var arg in Context.Scan.Arguments) {
				string @base = arg.Usage switch {
					DeclUsage.Position => "pos",
					DeclUsage.BlendWeight => "blweight",
					DeclUsage.BlendIndices => "blindex",
					DeclUsage.Normal => "normal",
					DeclUsage.Psize => "psize",
					DeclUsage.Texcoord => "uv",
					DeclUsage.Tangent => "tg",
					DeclUsage.Binormal => "binorm",
					DeclUsage.Tessfactor => "tess",
					DeclUsage.Positiont => "post",
					DeclUsage.Color => "color",
					DeclUsage.Fog => "fog",
					DeclUsage.Depth => "depth",
					DeclUsage.Sample => "sample",
					_ => "x"
				};

				bool withIndex = Context.Scan.Arguments.Any(inp => inp.Usage == arg.Usage && inp.UsageIndex != arg.UsageIndex);

				string name = @base;
				if (withIndex)
					name += arg.UsageIndex;

				if (Context.Shader.Constants.Any(c => c.Name == name)) {
					name = "arg_" + @base;
					if (withIndex)
						name += arg.UsageIndex;
				}

				Context.RegisterNames[(arg.RegisterType, arg.Register)] = name;
			}

			foreach (Constant constant in Context.Shader.Constants) {
				ParameterRegisterType type = constant.RegSet switch {
					RegSet.Sampler => ParameterRegisterType.Sampler,
					_ => ParameterRegisterType.Const
				};

				Context.RegisterNames[(type, constant.RegIndex)] = constant.Name!;
			}

			if (Context.Shader.Preshader is not null)
				foreach (Constant constant in Context.Shader.Preshader.Constants) {
					ParameterRegisterType type = constant.RegSet switch {
						RegSet.Sampler => ParameterRegisterType.Sampler,
						_ => ParameterRegisterType.PreshaderInput
					};

					Context.RegisterNames[(type, constant.RegIndex)] = constant.Name!;
				}
		}

		void CreateExpressionList() {
			Stack<Opcode> opcodes = new();

			for (int i = Context.Shader.Opcodes.Count - 1; i >= 0; i--)
				opcodes.Push(Context.Shader.Opcodes[i]);

			if (Context.Shader.Preshader is not null) {

				for (int i = Context.Shader.Preshader.Opcodes.Count - 1; i >= 0; i--)
					opcodes.Push(Context.Shader.Preshader.Opcodes[i]);
			}

			while (opcodes.Count > 0) {
				Expression? expr = CreateExpression(opcodes);
				if (expr is null)
					continue;

				Context.Expressions.Add(expr);
			}

			foreach (Expression? expr in Context.Expressions) {
				if (expr is AssignExpression assign) {
					assign.Source.MaskSwizzle(assign.Destination.WriteMask);
				}
			}

			foreach (var (type, index, _) in Context.Scan.RegistersReferenced) {
				Context.Scan.RegisterSizes.Remove((type, index));
			}
			
			foreach (Expression? expr in Context.Expressions) {
				if (expr is null)
					continue;
			
				foreach (var (type, index, _) in Context.Scan.RegistersReferenced) {
					SwizzleMask mask = expr.GetRegisterUsage(type, index, null);
					if (mask == SwizzleMask.None)
						continue;
			
					uint registerSize = mask.HasFlag(SwizzleMask.W) ? 4u
									  : mask.HasFlag(SwizzleMask.Z) ? 3u
									  : mask.HasFlag(SwizzleMask.Y) ? 2u
									  : 1u;
			
					Context.Scan.UpdateRegisterSize(type, index, registerSize);
				}
			}

			bool canSimplify = true;
			int cycle = -1;

			HashSet<(ParameterRegisterType Type, uint Index, bool Destination)> registerUsageCache = new();

			while (canSimplify) {
				cycle++;

				registerUsageCache.Clear();
				foreach (Expression? expr in Context.Expressions) {
					if (expr is null)
						continue;

					foreach (RegisterExpression register in expr.EnumerateRegisters()) {
						SwizzleMask mask = register.UsageMask;

						uint registerSize = mask.HasFlag(SwizzleMask.W) ? 4u
										  : mask.HasFlag(SwizzleMask.Z) ? 3u
										  : mask.HasFlag(SwizzleMask.Y) ? 2u
										  : 1u;

						var key = (register.Type, register.Index, register.Destination);
						if (!registerUsageCache.Contains(key)) {
							Context.Scan.RegisterSizes.Remove((register.Type, register.Index));
							registerUsageCache.Add(key);
						}

						Context.Scan.UpdateRegisterSize(register.Type, register.Index, registerSize);
					}
				}

				canSimplify = false;
				for (int i = 0; i < Context.Expressions.Count; i++) {
					Expression? expr = Context.Expressions[i];
					if (expr is null)
						continue;

					Context.CurrentExpressionIndex = i;
					bool tooComplex = expr.CalculateComplexity() > Context.Settings.ComplexityThreshold;

					Context.Expressions[i] = expr.Simplify(Context, !tooComplex && !Context.Settings.MinimumSimplifications, out bool fail);
					if (!fail)
						canSimplify = true;
				}

				for (int i = 0; i < Context.Expressions.Count; i++) {
					Context.CurrentExpressionIndex = i;
					if (Context.Expressions[i] is null || Context.Expressions[i]!.Clean(Context)) {
						Context.Expressions.RemoveAt(i);
						i--;
					}
				}
			}

			bool canClean = true;
			cycle = -1;
			while (canClean) {
				canClean = false;
				cycle++;
				//Console.WriteLine($"Cleaning cycle {cycle}: {Context.Expressions.Count} expressions");

				for (int i = 0; i < Context.Expressions.Count; i++) {
					Context.CurrentExpressionIndex = i;
					if (Context.Expressions[i] is null || Context.Expressions[i]!.Clean(Context)) {
						Context.Expressions.RemoveAt(i);
						i--;
						canClean = true;
					}
				}
			}
		}

		Expression? CreateExpression(Stack<Opcode> opcodes) {
			Opcode op = opcodes.Pop();

			Expression assign;

			switch (op.Type) {
				case OpcodeType.Nop:
				case OpcodeType.End:
				case OpcodeType.Comment:
				case OpcodeType.Dcl:
					return null;

				case OpcodeType.Def:
					if (Context.Scan.RegisterSizes.TryGetValue((op.Destination!.RegisterType, op.Destination!.Register), out uint size))
						size = 4;

					ConstantExpression[] values = op.Constant!.Take((int)size).Select(v => new ConstantExpression(v)).ToArray();
					assign = ComplexExpression.Create<ValueCtorExpression>(values);
					break;

				case OpcodeType.Rcp:
					assign = ComplexExpression.Create<DivisionExpression>(new ConstantExpression(1), op.Sources[0].ToExpr());
					break;
				case OpcodeType.AddScalar:
				case OpcodeType.Add:
					assign = op.Sources[0].ToExpr() + op.Sources[1].ToExpr();
					break;
				case OpcodeType.Sub:
					assign = op.Sources[0].ToExpr() - op.Sources[1].ToExpr();
					break;
				case OpcodeType.MulScalar:
				case OpcodeType.Mul:
					assign = op.Sources[0].ToExpr() * op.Sources[1].ToExpr();
					break;

				case OpcodeType.Neg:
					assign = -op.Sources[0].ToExpr();
					break;

				case OpcodeType.Mad:
					assign = (op.Sources[0].ToExpr() * op.Sources[1].ToExpr()) + op.Sources[2].ToExpr();
					break;

				case OpcodeType.Mov:
					assign = op.Sources[0].ToExpr();
					break;

				case OpcodeType.Lrp:
					assign = new CallExpression("lerp", op.Sources[0].ToExpr(), op.Sources[1].ToExpr(), op.Sources[2].ToExpr());
					break;

				case OpcodeType.Frc:
					assign = new CallExpression("frac", op.Sources[0].ToExpr());
					break;

				case OpcodeType.Texld:
					assign = new CallExpression("tex2D", op.Sources[1].ToExpr(), op.Sources[0].ToExpr());
					break;

				default:
					Expression[] args = op.Sources.Select(src => src.ToExpr()).ToArray();
					string name = op.Type.ToString().ToLower();
					assign = new CallExpression(name, args);
					break;

			}
			if (op.Destination is null)
				return assign;

			return ComplexExpression.Create<AssignExpression>(op.Destination!.ToExpr(), assign);
		}
	}
}
