using ShaderDecompiler.Decompilers;
using ShaderDecompiler.XNACompatibility;

namespace ShaderDecompiler;

public static partial class Program {
	public static void Main() {
		//string[] xnbFilenames = new string[] { "../../../TestData/Shader.xnb", "Shader.xnb" };
		//
		//foreach (string filename in xnbFilenames) {
		//	if (Path.Exists(filename)) {
		//		using FileStream fs = File.OpenRead(filename);
		//		using BinaryReader reader = new(fs);
		//
		//		Effect effect = XnbReader.ReadEffect(reader);
		//		File.WriteAllText(Path.Combine(Path.GetDirectoryName(filename)!, "Result.fx"), new EffectDecompiler(effect).Decompile());
		//	}
		//}

		string[] fxbFilenames = new string[] { "../../../TestData/Shader.fxb"};

		foreach (string filename in fxbFilenames) {
			if (Path.Exists(filename)) {
				using FileStream fs = File.OpenRead(filename);
				using BinaryReader reader = new(fs);

				Effect effect = Effect.Read(reader);
				File.WriteAllText(Path.Combine(Path.GetDirectoryName(filename)!, "Result.fx"), new EffectDecompiler(effect).Decompile());
			}
		}
	}
}
