using ShaderDecompiler.Decompiler.Expressions;
using ShaderDecompiler.Structures;
using System.Diagnostics;

// TODO Make register simplification smarter, accumulate used swizzles when scanning for used registers

namespace ShaderDecompiler.Decompiler
{
    public class Decompiler
    {
        CodeWriter Writer = new();
        HLSLEffect Effect = null!;

        public static string DecompieEffect(HLSLEffect effect)
        {
            Decompiler dc = new();
            dc.Effect = effect;
            dc.Decompile();

            return dc.Writer.ToString();
        }

        void Decompile()
        {
            foreach (Parameter param in Effect.Parameters)
                WriteNamedValue(param.Value);

            Writer.NewLine();

            foreach (Technique technique in Effect.Techniques)
            {
                foreach (Pass pass in technique.Passes)
                {
                    foreach (State state in pass.States)
                    {
                        state.Name = $"{technique.Name}{pass.Name}{state.Type}";

                        uint objIndex = (state.Value.Object as uint[])![0];
                        Shader? shader = Effect.Objects[objIndex].Object as Shader;

                        if (shader is null)
                        {
                            Writer.Write($"// shader {state.Name} could not be resolved\n");
                            continue;
                        }

                        ShaderDecompilationContext context = new(shader);
                        context.Scan = ScanShader(shader);

                        CreateShaderRegisterNames(context);

                        Writer.WriteSpaced("void");
                        Writer.WriteSpaced(state.Name);
                        Writer.Write("(");

                        bool firstArg = true;
                        foreach (var arg in context.Scan.Arguments)
                        {
                            Writer.LastSpace = true;
                            if (!firstArg)
                                Writer.Write(",");

                            if (arg.Output)
                                Writer.WriteSpaced(arg.Input ? "inout" : "out");

                            Writer.WriteSpaced("float");
                            uint argSize = arg.Size;
                            if (context.Scan.RegisterSizes.TryGetValue((arg.RegisterType, arg.Register), out uint regSize))
                                argSize = Math.Max(argSize, regSize);
                            if (argSize > 1)
                                Writer.Write(argSize.ToString());
                            Writer.WriteSpaced(context.RegisterNames[(arg.RegisterType, arg.Register)]);
                            Writer.WriteSpaced(":");
                            Writer.WriteSpaced(arg.Usage.ToString().ToUpper());
                            Writer.Write(arg.UsageIndex.ToString());

                            firstArg = false;
                        }
                        Writer.Write(")");
                        Writer.NewLine();

                        Writer.StartBlock("{", "}");
                        Writer.NewLine();

                        CreateExpressionList(context);

                        for (int i = 0; i < context.Expressions.Count; i++)
                        {
                            if (context.Expressions[i] is null)
                                continue;

                            context.CurrentExpressionIndex = i;
                            Writer.Write(context.Expressions[i].Decompile(context));
                            Writer.Write(";");
                            Writer.NewLine();
                        }

                        Writer.EndBlock();
                        Writer.NewLine();
                        Writer.NewLine();
                    }
                }
            }

            foreach (Technique technique in Effect.Techniques)
            {
                Writer.WriteSpaced("technique");
                Writer.WriteSpaced(technique.Name!);
                Writer.NewLine();
                Writer.StartBlock();
                Writer.NewLine();
                foreach (Pass pass in technique.Passes)
                {
                    Writer.WriteSpaced("pass");
                    Writer.WriteSpaced(pass.Name!);
                    Writer.NewLine();
                    Writer.StartBlock();
                    Writer.NewLine();
                    foreach (State state in pass.States)
                    {
                        uint objIndex = (state.Value.Object as uint[])![0];
                        Shader? shader = Effect.Objects[objIndex].Object as Shader;

                        if (shader is null)
                        {
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
        }

        void WriteNamedValue(Value value)
        {
            if (value.Name is null)
                return;

            Writer.WriteSpaced(value.Type.ToString());
            Writer.WriteSpaced(value.Name);

            if (value.Object is not null)
            {
                if (value.Type.Type >= ObjectType.Sampler && value.Type.Type <= ObjectType.Samplercube && value.Object is SamplerState[] states)
                {
                    Writer.WriteSpaced("= sampler_state\n");
                    Writer.StartBlock("{", "}");
                    Writer.NewLine();
                    foreach (SamplerState state in states)
                    {
                        Writer.WriteSpaced(state.Type.ToString());
                        WriteSimpleValueAssignment(state.Value, true);
                        Writer.Write(";");
                        Writer.NewLine();
                    }
                    Writer.EndBlock();
                }
                else
                {
                    WriteSimpleValueAssignment(value);
                }
            }

            Writer.Write(";");
            Writer.NewLine();
        }

        void WriteSimpleValueAssignment(Value value, bool useStringAngleBrackets = false)
        {
            object? obj = value.Object;
            if (value.Type.Class == ObjectClass.Object && obj is uint[] objIndexArray && objIndexArray.Length >= 1)
            {
                EffectObject effectObj = Effect.Objects[objIndexArray[0]];
                obj = effectObj.Object;
            }

            if (obj is null)
                return;

            if (obj is string str)
            {
                Writer.WriteSpaced("=");
                Writer.WriteSpaced(useStringAngleBrackets ? "<" : "\"");
                Writer.Write(str);
                Writer.Write(useStringAngleBrackets ? ">" : "\"");
            }

            else if (obj is Array array)
            {
                if (IsArrayEmptyOrDefault(array))
                    return;

                Writer.WriteSpaced("=");
                if (array.Length == 1)
                {
                    Writer.WriteSpaced(array.GetValue(0)?.ToString() ?? "null");
                    return;
                }

                Writer.WriteSpaced("{");
                for (int i = 0; i < array.Length; i++)
                {
                    if (i > 0)
                        Writer.Write(",");

                    Writer.WriteSpaced(array.GetValue(i)?.ToString() ?? "null");
                }
                Writer.WriteSpaced("}");
            }
            else
            {
                Debugger.Break();
            }
        }

        bool IsArrayEmptyOrDefault(Array array)
        {
            if (array.Length == 0)
                return true;

            Type elementType = array.GetType().GetElementType()!;
            object? @default = elementType.IsValueType ? Activator.CreateInstance(elementType) : null;

            for (int i = 0; i < array.Length; i++)
            {
                if (!Equals(array.GetValue(i), @default))
                    return false;
            }

            return true;
        }

        ShaderScanResult ScanShader(Shader shader)
        {
            ShaderScanResult result = new();

            foreach (Opcode op in shader.Opcodes)
            {
                if (op.Type == OpcodeType.Dcl && op.Extra.HasValue && op.Destination.HasValue)
                {
                    BitNumber dcl = new(op.Extra.Value);
                    DestinationParameter dest = op.Destination.Value;

                    ShaderArgument arg;

                    switch (dest.RegisterType)
                    {
                        case ParameterRegisterType.Input:
                            arg = result.GetArgument(dest.RegisterType, dest.Register);
                            arg.Usage = (DeclUsage)dcl[0..4];
                            arg.UsageIndex = dcl[16..19];
                            arg.Size = dest.WriteW ? 4u : dest.WriteZ ? 3u : dest.WriteY ? 2u : 1u;
                            arg.Input = true;
                            break;

                        case ParameterRegisterType.Output:
                            arg = result.GetArgument(dest.RegisterType, dest.Register);
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

                if (op.Destination.HasValue)
                {
                    DestinationParameter dest = op.Destination.Value;

                    result.RegistersReferenced.Add((dest.RegisterType, dest.Register, true));

                    uint registerSize = dest.WriteW ? 4u : dest.WriteZ ? 3u : dest.WriteY ? 2u : 1u;

                    switch (dest.RegisterType)
                    {
                        case ParameterRegisterType.Output:
                        case ParameterRegisterType.Attrout:
                            var arg = result.GetArgument(dest.RegisterType, dest.Register);
                            arg.Output = true;
                            arg.Size = Math.Max(arg.Size, registerSize);
                            break;
                    }

                    result.UpdateRegisterSize(dest.RegisterType, dest.Register, registerSize);
                }

                foreach (SourceParameter? src in op.Sources)
                {
                    if (!src.HasValue)
                        continue;

                    result.RegistersReferenced.Add((src.Value.RegisterType, src.Value.Register, false));

                    switch (src.Value.RegisterType)
                    {
                        case ParameterRegisterType.Input:
                            result.GetArgument(src.Value.RegisterType, src.Value.Register).Input = true;
                            break;
                    }

                    Swizzle maxSwizzle = (Swizzle)Math.Max(Math.Max((int)src.Value.SwizzleX, (int)src.Value.SwizzleY), Math.Max((int)src.Value.SwizzleZ, (int)src.Value.SwizzleW));
                    uint registerSize = (uint)maxSwizzle + 1;

                    result.UpdateRegisterSize(src.Value.RegisterType, src.Value.Register, registerSize);
                }
            }

            foreach (Constant constant in shader.Constants)
            {
                ParameterRegisterType type = constant.RegSet switch
                {
                    RegSet.Sampler => ParameterRegisterType.Sampler,
                    _ => ParameterRegisterType.Const
                };

                result.UpdateRegisterSize(type, constant.RegIndex, constant.TypeInfo.Columns);
            }

            foreach (var (type, index, dest) in result.RegistersReferenced)
            {
                if (dest && type == ParameterRegisterType.Colorout)
                {
                    ShaderArgument arg = result.GetArgument(ParameterRegisterType.Colorout, index);
                    arg.Output = true;
                    arg.Usage = DeclUsage.Color;
                    arg.UsageIndex = index;
                }
            }

            return result;
        }

        void CreateShaderRegisterNames(ShaderDecompilationContext context)
        {
            foreach (var arg in context.Scan.Arguments)
            {
                string @base = arg.Usage switch
                {
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

                bool withIndex = context.Scan.Arguments.Any(inp => inp.Usage == arg.Usage && inp.UsageIndex != arg.UsageIndex);

                string name = @base;
                if (withIndex)
                    name += arg.UsageIndex;

                if (context.Shader.Constants.Any(c => c.Name == name))
                {
                    name = "arg_" + @base;
                    if (withIndex)
                        name += arg.UsageIndex;
                }

                context.RegisterNames[(arg.RegisterType, arg.Register)] = name;
            }

            foreach (Constant constant in context.Shader.Constants)
            {
                ParameterRegisterType type = constant.RegSet switch
                {
                    RegSet.Sampler => ParameterRegisterType.Sampler,
                    _ => ParameterRegisterType.Const
                };

                context.RegisterNames[(type, constant.RegIndex)] = constant.Name!;
            }
        }

        void CreateExpressionList(ShaderDecompilationContext context)
        {
            Stack<Opcode> opcodes = new();

            for (int i = context.Shader.Opcodes.Count - 1; i > 0; i--)
                opcodes.Push(context.Shader.Opcodes[i]);

            while (opcodes.Count > 0)
            {
                Expression? expr = CreateExpression(opcodes, context);
                if (expr is null)
                    continue;

                context.Expressions.Add(expr);
            }

            foreach (Expression expr in context.Expressions)
            {
                if (expr is AssignExpression assign)
                {
                    assign.Source.MaskSwizzle(assign.Destination.WriteMask);
                }
            }

            bool canSimplify = true;
            List<int> removeIndexes = new();
            int cycle = -1;
            while (canSimplify)
            {
                cycle++;
                Console.WriteLine($"Simplification cycle {cycle}: {context.Expressions.Count} expressions");

                canSimplify = false;
                removeIndexes.Clear();
                for (int i = 0; i < context.Expressions.Count; i++)
                {
                    Expression? expr = context.Expressions[i];
                    if (expr is null)
                        continue;

                    context.CurrentExpressionIndex = i;
                    context.CurrentExpressionExceedsWeight = expr.CalculateWeight() > context.SimplificationWeightThreshold;
                    if (context.CurrentExpressionExceedsWeight && !expr.SimplifyOnWeightExceeded)
                        continue;

                    context.Expressions[i] = expr.Simplify(context, out bool fail);
                    if (!fail)
                        canSimplify = true;
                }

                for (int i = 0; i < context.Expressions.Count; i++)
                {
                    context.CurrentExpressionIndex = i;
                    if (context.Expressions[i] is null || context.Expressions[i]!.Clean(context))
                    {
                        context.Expressions.RemoveAt(i);
                        i--;
                    }
                }
            }

            bool canClean = true;
            cycle = -1;
            while (canClean)
            {
                canClean = false;
                cycle++;
                Console.WriteLine($"Cleaning cycle {cycle}: {context.Expressions.Count} expressions");

                for (int i = 0; i < context.Expressions.Count; i++)
                {
                    context.CurrentExpressionIndex = i;
                    if (context.Expressions[i] is null || context.Expressions[i]!.Clean(context))
                    {
                        context.Expressions.RemoveAt(i);
                        i--;
                        canClean = true;
                    }
                }
            }
        }

        Expression? CreateExpression(Stack<Opcode> opcodes, ShaderDecompilationContext context)
        {
            Opcode op = opcodes.Pop();

            Expression assign;

            switch (op.Type)
            {
                case OpcodeType.Nop:
                case OpcodeType.End:
                case OpcodeType.Comment:
                case OpcodeType.Dcl:
                    return null;
                    
                case OpcodeType.Def:
                    if (context.Scan.RegisterSizes.TryGetValue((op.Destination!.Value.RegisterType, op.Destination!.Value.Register), out uint size))
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
					assign = ComplexExpression.Create<SubstractionExpression>(op.Sources[0].ToExpr(), op.Sources[1].ToExpr());
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
