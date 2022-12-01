namespace ShaderDecompiler.Structures {
	public class EffectObject {
		public ObjectType Type;
		public object? Object;

		public override string ToString() {
			return Type.ToString();
		}
	}
}
