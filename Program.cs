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

		string output;

		if (xnb is not null) {
			using FileStream fs = File.OpenRead(xnb);
			using BinaryReader reader = new(fs);
			Effect effect = XnbReader.ReadEffect(reader);
			output = new EffectDecompiler(effect).Decompile();
		}
		else if (fx is not null) {
			using FileStream fs = File.OpenRead(fx);
			using BinaryReader reader = new(fs);
			Effect effect = Effect.Read(reader);
			output = new EffectDecompiler(effect).Decompile();
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
