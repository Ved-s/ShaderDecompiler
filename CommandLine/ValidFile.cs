using System.Reflection;

namespace ShaderDecompiler.CommandLine {
	public class ValidFile : ArgumentValueModifier {
		public override string Name => "FilePath";

		static HashSet<char> InvalidFilePathChars = new HashSet<char>();

		static ValidFile() {
			InvalidFilePathChars.UnionWith(Path.GetInvalidPathChars());
			InvalidFilePathChars.UnionWith(Path.GetInvalidFileNameChars());
			InvalidFilePathChars.Remove('/');
			InvalidFilePathChars.Remove('\\');
		}

		public override bool CanModify(Type type) => type == typeof(string);

		public override object? Modify(CommandContext context, object? value, ParameterInfo parameter) {
			string path = (string)value!;
			if (!ValidateFilePath(path))
				throw new Exception($"{path} is not a valid path");
			return value;
		}

		public static bool ValidateFilePath(string path) {
			return path.All(c => !InvalidFilePathChars.Contains(c));
		}
	}
}
