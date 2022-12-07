#region License
/*
  ShaderDecompiler - Direct3D shader decompiler

  Released under Microsoft Public License
  See LICENSE for details
*/
#endregion

namespace ShaderDecompiler.Structures;

public struct ShaderVersion {
	public ShaderType Type;
	public uint Minor, Major;

	public static ShaderVersion Read(BinaryReader reader) {
		uint token = reader.ReadUInt32();

		ShaderVersion version = new();
		version.Type = (ShaderType)((token & 0xffff0000) >> 16);
		if (!Enum.IsDefined(version.Type))
			version.Type = ShaderType.Unknown;

		version.Minor = token & 0xff;
		version.Major = (token & 0xff00) >> 8;

		return version;
	}

	public bool CheckVersionGreaterOrEqual(ShaderType type, uint major, uint minor) {
		if (Type != type)
			return false;
		return Major >= major && Minor >= minor;
	}

	public bool CheckVersionLess(ShaderType type, uint major, uint minor) {
		if (Type != type)
			return false;

		return Major < major && Minor < minor;
	}

	public override string ToString() {
		return $"{Type} v{Major}.{Minor}";
	}

	public override bool Equals(object? obj) {
		return obj is ShaderVersion other && this == other;
	}

	public override int GetHashCode() {
		return HashCode.Combine(Type, Minor, Major);
	}

	public static bool operator ==(ShaderVersion a, ShaderVersion b) => a.Type == b.Type && a.Minor == b.Minor && a.Major == b.Major;
	public static bool operator !=(ShaderVersion a, ShaderVersion b) => a.Type != b.Type || a.Minor != b.Minor || a.Major != b.Major;
}

public enum ShaderType : ushort {
	Unknown = 0,
	PixelShader = 0xffff,
	VertexShader = 0xfffe,
	Preshader = 0x4658
}
