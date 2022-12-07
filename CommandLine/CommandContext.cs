#region License
/*
  ShaderDecompiler - Direct3D shader decompiler

  Released under Microsoft Public License
  See LICENSE for details
*/
#endregion

namespace ShaderDecompiler.CommandLine {
	public class CommandContext {
		public CommandCaller Caller { get; set; } = new ConsoleCaller();
		public Dictionary<string, object?> ArgumentCache { get; set; } = new();
	}
}
