namespace ShaderDecompiler.Structures {
	public class Parameter : AnnotatedObject {
		public uint Flags;
		public Value Value = null!;

		public override string ToString() {
			return Value.ToString();
		}
	}
}
