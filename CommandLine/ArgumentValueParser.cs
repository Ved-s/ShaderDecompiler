namespace ShaderDecompiler.CommandLine {
	public abstract class ArgumentValueParser {
		public abstract bool CanReadType(Type type);

		public abstract bool TryRead(CommandContext context, CommandReader reader, bool shortArg, out object? value);
	}
}
