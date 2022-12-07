#region License
/*
  ShaderDecompiler - Direct3D shader decompiler

  Released under Microsoft Public License
  See LICENSE for details
*/
#endregion

using ShaderDecompiler.CommandLine;
using ShaderDecompiler.Decompilers;
using ShaderDecompiler.XNACompatibility;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ShaderDecompiler;

public static partial class Program {
	public static void Main() {
		string cmdl = Environment.CommandLine;

		string[] argSplit = cmdl.Split(' ', 2);
		string appName = Path.GetFileNameWithoutExtension(argSplit[0]);

		string args = argSplit.Length == 1 ? "" : argSplit[1];

		CommandLine.Command cmd = new(appName) {
			Description = "HLSL Shader decompilation utility",
			Arguments = {
				new("help") { ShortName = 'h', Optional = true },

				new("xnb", typeof(string)) {
					ShortName = 'x',
					Optional = true,
					Description = "XNB file to extract and decompile",
					Modifier = new ExistingFile()
				},
				new("fx", typeof(string)) {
					ShortName = 'f',
					Optional = true,
					Description = "Effect object file to decompile",
					Modifier = new ExistingFile()
				},
				new("out", typeof(string)) {
					ShortName = 'o',
					Optional = true,
					Description = "Output file",
					Modifier = new ValidFile()
				},

				new("minSimplify") {
					ShortName = 'm',
					Optional = true,
					Description = "Disable most of simplification algorithms",
				},
				new("complexityThreshold", typeof(int)) {
					ShortName = 't',
					Optional = true,
					Description = "Define complexity threshold",
				},

				new("decompilationFilter", typeof(string)) {
					ShortName = 'd',
					Optional = true,
					Description = 
					"Filter techniques, passes and shaders to decomple\n" +
					"Format: Technique/Pass/Shader\n" +
					"Shader should be either PixelShader, VertexShader\n" +
					"Supports regular expressions"
				}
			},
			ExecutionMethod = MainCommand
		};
		try {
			cmd.Execute(new(), args);
		}
		catch (Exception e) { 
			Console.WriteLine(e.ToString());
		}
	}

	static void MainCommand(Command cmd, CommandContext ctx, string? xnb = null, string? fx = null, string? @out = null) {
		if (ctx.ArgumentCache.Count == 0 || ctx.ArgumentCache.ContainsKey("help")) {
			ctx.Caller.Respond(cmd.CreateHelp());
			return;
		}

		DecompilationSettings settings = new();
		if (ctx.ArgumentCache.ContainsKey("minSimplify"))
			settings.MinimumSimplifications = true;

		if (ctx.ArgumentCache.TryGetValue("complexityThreshold", out object? threshold))
			settings.ComplexityThreshold = threshold as int? ?? int.MaxValue;

		if (ctx.ArgumentCache.TryGetValue("decompilationFilter", out object? filter))
			settings.ShaderPathFilter = new Regex((string)filter!, RegexOptions.Compiled);

		string output;

		if (xnb is not null) {
			using FileStream fs = File.OpenRead(xnb);
			using BinaryReader reader = new(fs);
			Effect effect = XnbReader.ReadEffect(reader);
			output = new EffectDecompiler(effect).Decompile(settings);
		}
		else if (fx is not null) {
			using FileStream fs = File.OpenRead(fx);
			using BinaryReader reader = new(fs);
			Effect effect = Effect.Read(reader);
			output = new EffectDecompiler(effect).Decompile(settings);
		}
		else {
			ctx.Caller.Respond("No input file specified");
			return;
		}

		if (@out is not null)
			File.WriteAllText(@out, output);
		else 
			Console.WriteLine(output);

	}
}
