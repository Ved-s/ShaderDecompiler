#region License
/*
  ShaderDecompiler - Direct3D shader decompiler

  Released under Microsoft Public License
  See LICENSE for details
*/
#endregion

using System.IO;
using System.Reflection;

namespace ShaderDecompiler.CommandLine {
	public class ExistingFile : ArgumentValueModifier {
		public override string Name => "ExistingFilePath";

		public override bool CanModify(Type type) => type == typeof(string);
		public override object? Modify(CommandContext context, object? value, ParameterInfo parameter) {
			string path = (string)value!;
			if (!ValidFile.ValidateFilePath(path))
				throw new Exception($"{path} is not a valid path");
			if (!File.Exists(path))
				throw new FileNotFoundException($"Argument {parameter.Name} expected an existing file");
			return value;
		}
	}
}
