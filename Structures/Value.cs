namespace ShaderDecompiler.Structures {
	public class Value {
		public TypeInfo Type = new();
		public object? Object;
		public string? Name;
		public string? Semantic;

		public override string ToString() {
			return $"{Type} {Name}{(Semantic is null ? "" : $" : {Semantic}")};";
		}
	}
}
