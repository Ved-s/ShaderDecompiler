using ShaderDecompiler.Decompiler;

namespace ShaderDecompiler;

public static partial class Program {
	public static void Main() {
		string filename = "../../../TestData/Shader.fxb";
		if (!File.Exists(filename))
			filename = "TestData/Shader.fxb";

		FileStream fs = File.OpenRead(filename);
		BinaryReader reader = new(fs);

		Effect effect = Effect.Read(reader);

		reader.Close();
		fs.Dispose();

		File.WriteAllText(Path.Combine(Path.GetDirectoryName(filename)!, "Result.fx"), new EffectDecompiler(effect).Decompile());
	}
}
