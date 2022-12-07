#region License
/*
  ShaderDecompiler - Direct3D shader decompiler

  Released under Microsoft Public License
  See LICENSE for details
*/
#endregion

using System.Reflection;

namespace ShaderDecompiler.CommandLine {
	public class ValidFile : ArgumentValueModifier {
		public override string Name => "FilePath";

		static HashSet<char> InvalidFilePathChars = new HashSet<char>(Path.GetInvalidPathChars());
		static HashSet<char> InvalidFileNameChars = new HashSet<char>(Path.GetInvalidFileNameChars());

		public override bool CanModify(Type type) => type == typeof(string);

		public override object? Modify(CommandContext context, object? value, ParameterInfo parameter) {
			string path = (string)value!;
			if (!ValidateFilePath(path))
				throw new Exception($"{path} is not a valid path");
			return value;
		}

		public static bool ValidateFilePath(string path) {
			string? dir = Path.GetDirectoryName(path);
			if (dir is not null && dir.Any(c => InvalidFilePathChars.Contains(c)))
				return false;

			string? file = Path.GetFileName(path);
			return file is not null && file.All(c => !InvalidFileNameChars.Contains(c));
		}
	}
}
