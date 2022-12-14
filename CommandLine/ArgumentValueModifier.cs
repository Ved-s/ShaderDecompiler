#region License
/*
  ShaderDecompiler - Direct3D shader decompiler

  Released under Microsoft Public License
  See LICENSE for details
*/
#endregion

using System.Reflection;

namespace ShaderDecompiler.CommandLine {
	public abstract class ArgumentValueModifier {
		public abstract string Name { get; }
		public virtual string? Description { get; }

		public abstract bool CanModify(Type type);
		public abstract object? Modify(CommandContext context, object? value, ParameterInfo parameter);
	}
}
