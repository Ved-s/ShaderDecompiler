namespace ShaderDecompiler.CommandLine {
	public class CommandContext {
		public CommandCaller Caller { get; set; } = new ConsoleCaller();
		public Dictionary<string, object?> ArgumentCache { get; set; } = new();
	}
}
