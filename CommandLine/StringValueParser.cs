using System.Text.RegularExpressions;

namespace ShaderDecompiler.CommandLine {
	public class StringValueParser : ArgumentValueParser {
		static Regex StringRegex = new(@"""(.*?)(?<!\\)""|(\S+)", RegexOptions.Compiled);

		public override bool CanReadType(Type type) {
			return type == typeof(string);
		}

		public override bool TryRead(CommandContext context, CommandReader reader, bool shortArg, out object? value) {
			value = null;
			if (!reader.TryMatch(StringRegex, out Match match, true))
				return false;

			value = match.Groups[match.Groups[1].Success ? 1 : 2].Value;
			return true;
		}
	}
}
