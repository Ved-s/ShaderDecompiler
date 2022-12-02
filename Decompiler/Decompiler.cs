using ShaderDecompiler.Decompiler.Expressions;
using ShaderDecompiler.Structures;
using System.Diagnostics;

namespace ShaderDecompiler.Decompiler {
	public class EffectDecompiler {
		CodeWriter Writer = new();
		Effect Effect;

		public EffectDecompiler(Effect effect) {
			Effect = effect;
		}

		public string Decompile() {
			Writer.Clear();

			foreach (Parameter param in Effect.Parameters)
				WriteNamedValue(param.Value);

			Writer.NewLine();

			foreach (Technique technique in Effect.Techniques) {
				foreach (Pass pass in technique.Passes) {
					foreach (State state in pass.States) {
						state.Name = $"{technique.Name}{pass.Name}{state.Type}";

						uint objIndex = (state.Value.Object as uint[])![0];

						if (Effect.Objects[objIndex].Object is not Shader shader) {
							Writer.Write($"// shader {state.Name} could not be resolved\n");
							continue;
						}

						ShaderDecompiler decompiler = new(shader);
						decompiler.Decompile(Writer, state.Name);
						
						Writer.NewLine();
						Writer.NewLine();
					}
				}
			}

			foreach (Technique technique in Effect.Techniques) {
				Writer.WriteSpaced("technique");
				Writer.WriteSpaced(technique.Name!);
				Writer.NewLine();
				Writer.StartBlock();
				Writer.NewLine();
				foreach (Pass pass in technique.Passes) {
					Writer.WriteSpaced("pass");
					Writer.WriteSpaced(pass.Name!);
					Writer.NewLine();
					Writer.StartBlock();
					Writer.NewLine();
					foreach (State state in pass.States) {
						uint objIndex = (state.Value.Object as uint[])![0];

						if (Effect.Objects[objIndex].Object is not Shader shader) {
							Writer.Write($"\t\t// could not resolve {state.Type} {state.Name}");
							continue;
						}

						Writer.WriteSpaced(state.Type.ToString());
						Writer.WriteSpaced("=");
						Writer.WriteSpaced("compile");
						Writer.WriteSpaced(shader.Target!);
						Writer.WriteSpaced(state.Name!);
						Writer.Write("();");
						Writer.NewLine();
					}
					Writer.EndBlock();
					Writer.NewLine();
				}
				Writer.EndBlock();
				Writer.NewLine();
			}

			return Writer.ToString();
		}

		void WriteNamedValue(Value value) {
			if (value.Name is null)
				return;

			Writer.WriteSpaced(value.Type.ToString());
			Writer.WriteSpaced(value.Name);

			if (value.Object is not null) {
				if (value.Type.Type >= ObjectType.Sampler && value.Type.Type <= ObjectType.Samplercube && value.Object is SamplerState[] states) {
					Writer.WriteSpaced("= sampler_state\n");
					Writer.StartBlock("{", "}");
					Writer.NewLine();
					foreach (SamplerState state in states) {
						Writer.WriteSpaced(state.Type.ToString());
						WriteSimpleValueAssignment(state.Value, true);
						Writer.Write(";");
						Writer.NewLine();
					}
					Writer.EndBlock();
				}
				else {
					WriteSimpleValueAssignment(value);
				}
			}

			Writer.Write(";");
			Writer.NewLine();
		}

		void WriteSimpleValueAssignment(Value value, bool useStringAngleBrackets = false) {
			object? obj = value.Object;
			if (value.Type.Class == ObjectClass.Object && obj is uint[] objIndexArray && objIndexArray.Length >= 1) {
				EffectObject effectObj = Effect.Objects[objIndexArray[0]];
				obj = effectObj.Object;
			}

			if (obj is null)
				return;

			if (obj is string str) {
				Writer.WriteSpaced("=");
				Writer.WriteSpaced(useStringAngleBrackets ? "<" : "\"");
				Writer.Write(str);
				Writer.Write(useStringAngleBrackets ? ">" : "\"");
			}

			else if (obj is Array array) {
				if (IsArrayEmptyOrDefault(array))
					return;

				Writer.WriteSpaced("=");
				if (array.Length == 1) {
					Writer.WriteSpaced(array.GetValue(0)?.ToString() ?? "null");
					return;
				}

				Writer.WriteSpaced("{");
				for (int i = 0; i < array.Length; i++) {
					if (i > 0)
						Writer.Write(",");

					Writer.WriteSpaced(array.GetValue(i)?.ToString() ?? "null");
				}
				Writer.WriteSpaced("}");
			}
			else {
				Debugger.Break();
			}
		}

		bool IsArrayEmptyOrDefault(Array array) {
			if (array.Length == 0)
				return true;

			Type elementType = array.GetType().GetElementType()!;
			object? @default = elementType.IsValueType ? Activator.CreateInstance(elementType) : null;

			for (int i = 0; i < array.Length; i++) {
				if (!Equals(array.GetValue(i), @default))
					return false;
			}

			return true;
		}

		
	}

	public class ShaderDecompiler {
		CodeWriter Writer = null!;
		Shader Shader = null!;
		ShaderDecompilationContext Context = null!;

		public ShaderDecompiler(Shader shader) {
			Shader = shader;
			
		}

		public void Decompile(CodeWriter writer, string entryPointName) {
			Writer = writer;
			Context = new(Shader);

			ScanShader();
			CreateShaderRegisterNames();

			Writer.WriteSpaced("void");
			Writer.WriteSpaced(entryPointName);
			Writer.Write("(");

			bool firstArg = true;
			foreach (var arg in Context.Scan.Arguments) {
				Writer.LastSpace = true;
				if (!firstArg)
					Writer.Write(",");

				if (arg.Output)
					Writer.WriteSpaced(arg.Input ? "inout" : "out");

				Writer.WriteSpaced("float");
				uint argSize = arg.Size;
				if (Context.Scan.RegisterSizes.TryGetValue((arg.RegisterType, arg.Register), out uint regSize))
					argSize = Math.Max(argSize, regSize);
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

			CreateExpressionList();

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

			foreach (Opcode op in Shader.Opcodes) {
				if (op.Type == OpcodeType.Dcl && op.Extra.HasValue && op.Destination.HasValue) {
					BitNumber dcl = new(op.Extra.Value);
					DestinationParameter dest = op.Destination.Value;

					ShaderArgument arg;

					switch (dest.RegisterType) {
						case ParameterRegisterType.Input:
							arg = Context.Scan.GetArgument(dest.RegisterType, dest.Register);
							arg.Usage = (DeclUsage)dcl[0..4];
							arg.UsageIndex = dcl[16..19];
							arg.Size = dest.WriteW ? 4u : dest.WriteZ ? 3u : dest.WriteY ? 2u : 1u;
							arg.Input = true;
							break;

						case ParameterRegisterType.Output:
							arg = Context.Scan.GetArgument(dest.RegisterType, dest.Register);
							arg.Output = true;
							break;

						case ParameterRegisterType.Misctype:
						case ParameterRegisterType.Sampler:
							break;

						default:
							Debugger.Break();
							break;
					}
				}

				if (op.Destination.HasValue) {
					DestinationParameter dest = op.Destination.Value;

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
					if (!src.HasValue)
						continue;

					Context.Scan.RegistersReferenced.Add((src.Value.RegisterType, src.Value.Register, false));

					switch (src.Value.RegisterType) {
						case ParameterRegisterType.Input:
							Context.Scan.GetArgument(src.Value.RegisterType, src.Value.Register).Input = true;
							break;
					}

					Swizzle maxSwizzle = (Swizzle)Math.Max(Math.Max((int)src.Value.SwizzleX, (int)src.Value.SwizzleY), Math.Max((int)src.Value.SwizzleZ, (int)src.Value.SwizzleW));
					uint registerSize = (uint)maxSwizzle + 1;

					Context.Scan.UpdateRegisterSize(src.Value.RegisterType, src.Value.Register, registerSize);
				}
			}

			foreach (Constant constant in Shader.Constants) {
				ParameterRegisterType type = constant.RegSet switch {
					RegSet.Sampler => ParameterRegisterType.Sampler,
					_ => ParameterRegisterType.Const
				};
			}

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
		}

		void CreateExpressionList() {
			Stack<Opcode> opcodes = new();

			for (int i = Context.Shader.Opcodes.Count - 1; i > 0; i--)
				opcodes.Push(Context.Shader.Opcodes[i]);

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
			List<int> removeIndexes = new();
			int cycle = -1;
			while (canSimplify) {
				cycle++;
				Console.WriteLine($"Simplification cycle {cycle}: {Context.Expressions.Count} expressions");

				canSimplify = false;
				removeIndexes.Clear();
				for (int i = 0; i < Context.Expressions.Count; i++) {
					Expression? expr = Context.Expressions[i];
					if (expr is null)
						continue;

					Context.CurrentExpressionIndex = i;
					bool tooComplex = expr.CalculateComplexity() > Context.ComplexityThreshold;

					Context.Expressions[i] = expr.Simplify(Context, !tooComplex, out bool fail);
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
				Console.WriteLine($"Cleaning cycle {cycle}: {Context.Expressions.Count} expressions");

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
					if (Context.Scan.RegisterSizes.TryGetValue((op.Destination!.Value.RegisterType, op.Destination!.Value.Register), out uint size))
						size = 4;

					ConstantExpression[] values = op.Constant!.Take((int)size).Select(v => new ConstantExpression(v)).ToArray();
					assign = ComplexExpression.Create<ValueCtorExpression>(values);
					break;

				case OpcodeType.Rcp:
					assign = ComplexExpression.Create<DivisionExpression>(new ConstantExpression(1), op.Sources[0].ToExpr());
					break;
				case OpcodeType.Add:
					assign = ComplexExpression.Create<AdditionExpression>(op.Sources[0].ToExpr(), op.Sources[1].ToExpr());
					break;
				case OpcodeType.Sub:
					assign = ComplexExpression.Create<SubtractionExpression>(op.Sources[0].ToExpr(), op.Sources[1].ToExpr());
					break;
				case OpcodeType.Mul:
					assign = ComplexExpression.Create<MultiplicationExpression>(op.Sources[0].ToExpr(), op.Sources[1].ToExpr());
					break;

				case OpcodeType.Mov:
					assign = op.Sources[0].ToExpr();
					break;

				case OpcodeType.Lrp:
					assign = new CallExpression("lerp", op.Sources[0].ToExpr(), op.Sources[1].ToExpr(), op.Sources[2].ToExpr());
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
			if (!op.Destination.HasValue)
				return assign;

			return ComplexExpression.Create<AssignExpression>(op.Destination!.Value.ToExpr(), assign);
		}
	}
}
