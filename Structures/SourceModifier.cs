namespace ShaderDecompiler.Structures;

public enum SourceModifier : uint {
	None,
	Negate,
	Bias,
	BiasNegate,
	Sign,
	SignNegate,
	Complement,
	Double,
	DoubleNegate,
	DivideByZ,
	DivideByW,
	Abs,
	AbsNegate,
	LogicalNot
}
