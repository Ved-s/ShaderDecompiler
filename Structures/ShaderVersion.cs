namespace ShaderDecompiler.Structures;

public struct ShaderVersion {
	public bool? PixelShader;
	public uint Minor, Major;

	public static ShaderVersion Read(BinaryReader reader) {
		uint token = reader.ReadUInt32();

		ShaderVersion version = new();
		uint type = (token & 0xffff0000) >> 16;
		if (type == 0xffff)
			version.PixelShader = true;
		else if (type == 0xfffe)
			version.PixelShader = false;
		else
			version.PixelShader = null;

		version.Minor = token & 0xff;
		version.Major = (token & 0xff00) >> 8;

		return version;
	}

	public override string ToString() {
		return $"{(PixelShader is null ? "Unknown" : PixelShader is true ? "PixelShader" : "VertexShader")} v{Major}.{Minor}";
	}

	public override bool Equals(object? obj) {
		return obj is ShaderVersion other && this == other;
	}

	public static bool operator ==(ShaderVersion a, ShaderVersion b) => a.PixelShader == b.PixelShader && a.Minor == b.Minor && a.Major == b.Major;
	public static bool operator !=(ShaderVersion a, ShaderVersion b) => a.PixelShader != b.PixelShader || a.Minor != b.Minor || a.Major != b.Major;
}
