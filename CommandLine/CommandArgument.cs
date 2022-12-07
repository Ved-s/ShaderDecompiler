#region License
/*
  ShaderDecompiler - Direct3D shader decompiler

  Released under Microsoft Public License
  See LICENSE for details
*/
#endregion

namespace ShaderDecompiler.CommandLine {
	public class CommandArgument {
		static List<ArgumentValueParser> ArgumentParsers = new()
		{
			new StringValueParser(),
			new DBNullValueParser(),
			new IntValueParser()
		};

		public string Name { get; init; }
		public string? Description { get; init; }
		public Type Type { get; init; }
		public bool Optional { get; set; } = false;

		public ArgumentValueModifier? Modifier { get; set; }

		public char? ShortName { get; set; }

		private ArgumentValueParser? Parser;

		public CommandArgument(string name, Type? type = null) {
			Name = name;
			Type = type ?? typeof(DBNull);
		}

		public void Construct(CommandContext context) {
			if (Parser is not null || Type is null)
				return;

			Parser = ArgumentParsers.FirstOrDefault(p => p.CanReadType(Type));
			if (Parser is null)
				throw new Exception($"No registered parsers for {Type.Name}");

			if (Modifier is not null && !Modifier.CanModify(Type))
				throw new Exception($"Unsupported argument type {Type.Name} for modifier {Modifier.Name}");

			return;
		}

		public bool TryRead(CommandContext context, CommandReader reader, bool shortArg, out object? value) {
			if (Parser is null) {
				context.Caller.Respond($"Argument {Name} is not constructed");
				value = null;
				return false;
			}

			return Parser.TryRead(context, reader, shortArg, out value);
		}
	}
}
