using ShaderDecompiler.Decompilers;
using ShaderDecompiler.XNACompatibility;

namespace ShaderDecompiler;

public static partial class Program {
	public static void Main() {
		string[] filenames = new string[] { "../../../TestData/Shader.xnb", "Shader.xnb" };

		foreach (string filename in filenames) {
			if (Path.Exists(filename)) {
				using FileStream fs = File.OpenRead(filename);
				using BinaryReader reader = new(fs);

				Effect effect = XnbReader.ReadEffect(reader);
				File.WriteAllText(Path.Combine(Path.GetDirectoryName(filename)!, "Result.fx"), new EffectDecompiler(effect).Decompile());
			}
		}
	}
}
