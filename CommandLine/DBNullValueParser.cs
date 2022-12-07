#region License
/*
  ShaderDecompiler - Direct3D shader decompiler

  Released under Microsoft Public License
  See LICENSE for details
*/
#endregion

namespace ShaderDecompiler.CommandLine {
	public class DBNullValueParser : ArgumentValueParser {
		public override bool CanReadType(Type type) {
			return type == typeof(DBNull);
		}

		public override bool TryRead(CommandContext context, CommandReader reader, bool shortArg, out object? value) {
			value = null;
			return true;
		}
	}
}
