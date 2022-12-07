#region License
/*
  ShaderDecompiler - Direct3D shader decompiler

  Released under Microsoft Public License
  See LICENSE for details
*/
#endregion

using ShaderDecompiler.Structures;
using System.Diagnostics;

namespace ShaderDecompiler.Decompilers {
	public class EffectDecompiler {
		CodeWriter Writer = new();
		Effect Effect;

		public EffectDecompiler(Effect effect) {
			Effect = effect;
		}

		public string Decompile(DecompilationSettings? settings = null) {
			Writer.Clear();

			foreach (Parameter param in Effect.Parameters)
				WriteNamedValue(param.Value);

			Writer.NewLine();

			foreach (Technique technique in Effect.Techniques) {
				foreach (Pass pass in technique.Passes) {
					foreach (State state in pass.States) {
						if (settings?.ShaderPathFilter is not null
						 && !settings.ShaderPathFilter.IsMatch($"{technique.Name}/{pass.Name}/{state.Type}")) {
							state.Ignored = true;
							continue;
						}

						state.Name = $"{technique.Name}{pass.Name}{state.Type}";

						uint objIndex = (state.Value.Object as uint[])![0];

						if (Effect.Objects[objIndex].Object is not Shader shader) {
							Writer.Write($"// shader {state.Name} could not be resolved\n");
							continue;
						}

						ShaderDecompiler decompiler = new(shader);
						decompiler.Decompile(Writer, state.Name, settings);
						
						Writer.NewLine();
						Writer.NewLine();
					}
				}
			}

			foreach (Technique technique in Effect.Techniques) {
				if (technique.Passes.All(p => p.States.All(s => s.Ignored)))
					continue;

				Writer.WriteSpaced("technique");
				Writer.WriteSpaced(technique.Name!);
				Writer.NewLine();
				Writer.StartBlock();
				Writer.NewLine();
				foreach (Pass pass in technique.Passes) {
					if (pass.States.All(s => s.Ignored))
						continue;

					Writer.WriteSpaced("pass");
					Writer.WriteSpaced(pass.Name!);
					Writer.NewLine();
					Writer.StartBlock();
					Writer.NewLine();
					foreach (State state in pass.States) {
						if (state.Ignored)
							continue;

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
}
