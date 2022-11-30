using ShaderDecompiler.Structures;

namespace ShaderDecompiler.Decompiler
{
    public struct ShaderScanResult
    { 
        public readonly List<ShaderArgument> Arguments = new();
        public readonly HashSet<(ParameterRegisterType type, uint index, bool dest)> RegistersReferenced = new();
        public readonly Dictionary<(ParameterRegisterType, uint), uint> RegisterSizes = new();

        public ShaderScanResult()
        {
        }

        public ShaderArgument GetArgument(ParameterRegisterType type, uint register)
        {
            ShaderArgument? arg = Arguments.FirstOrDefault(arg => arg.RegisterType == type && arg.Register == register);
            if (arg is null)
            {
                arg = new()
                {
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
    }


}
