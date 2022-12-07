#region License
/*
  ShaderDecompiler - Direct3D shader decompiler

  Released under Microsoft Public License
  See LICENSE for details
*/
#endregion

using System.Text.RegularExpressions;

namespace ShaderDecompiler.CommandLine {
	public class IntValueParser : ArgumentValueParser {
		static Regex IntRegex = new(@"-?\d+", RegexOptions.Compiled);

		public override bool CanReadType(Type type) {
			return type == typeof(int);
		}

		public override bool TryRead(CommandContext context, CommandReader reader, bool shortArg, out object? value) {
			value = null;
			if (!reader.TryMatch(IntRegex, out Match match, true))
				return false;

			if (!int.TryParse(match.Value, out int intValue))
				return false;

			value = intValue;
			return true;
		}
	}
}
