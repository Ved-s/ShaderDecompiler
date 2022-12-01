namespace ShaderDecompiler.Decompiler {
	[Flags]
	public enum SwizzleMask : byte {
		None = 0,

		X = 1,
		Y = 2,
		Z = 4,
		W = 8,

		// TODO: complex expressions mask overrides for specific parameters
		// dest.z = dp4(a.xyzw, b.xyzw) -> dp4(a.z, b.xyzw) (arg 0 -> dest mask, arg 1 -> full mask)
	}
}
