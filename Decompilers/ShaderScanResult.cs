#region License
/*
  ShaderDecompiler - Direct3D shader decompiler

  Released under Microsoft Public License
  See LICENSE for details
*/
#endregion

using ShaderDecompiler.Structures;

namespace ShaderDecompiler.Decompilers {
	public class ShaderScanResult {
		public readonly List<ShaderArgument> Arguments = new();
		public readonly HashSet<(ParameterRegisterType type, uint index, bool dest)> RegistersReferenced = new();
		public readonly Dictionary<(ParameterRegisterType, uint), uint> RegisterSizes = new();
		public readonly HashSet<uint> DeclaredConstants = new();

		public ShaderScanResult() {
		}

		public ShaderArgument GetArgument(ParameterRegisterType type, uint register) {
			ShaderArgument? arg = Arguments.FirstOrDefault(arg => arg.RegisterType == type && arg.Register == register);
			if (arg is null) {
				arg = new() {
					Register = register,
					RegisterType = type,
					Usage = DeclUsage.Unknown,
					UsageIndex = (uint)Arguments.Count(arg => arg.Usage == DeclUsage.Unknown),
					Input = false,
					Output = false,
					Size = 1
				};
				Arguments.Add(arg);
			}
			return arg;
		}

		public void UpdateRegisterSize(ParameterRegisterType type, uint index, uint size, bool @override = false) {
			if (!RegisterSizes.TryGetValue((type, index), out uint regMaxSize))
				regMaxSize = 1;

			RegisterSizes[(type, index)] = Math.Max(regMaxSize, size);
		}
	}
}
