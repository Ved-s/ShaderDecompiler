#region License
/*
  ShaderDecompiler - Direct3D shader decompiler

  Released under Microsoft Public License
  See LICENSE for details
*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ShaderDecompiler.CommandLine {
	public class Command {
		static Regex LongArgName = new(@"--(\w+) ?", RegexOptions.Compiled);
		static Regex ShortArgName = new(@"[/-](\w+) ?", RegexOptions.Compiled);

		public string Name { get; set; }
		public string? Description { get; set; }

		public List<CommandArgument> Arguments { get; set; } = new();
		public Delegate? ExecutionMethod { get; set; }

		private Func<CommandContext, object?>? MethodCaller;

		public Command(string name) {
			Name = name;
		}

		public object? Execute(CommandContext context, string args) {
			Construct(context);

			if (Arguments.Count > 0 && Arguments.Any(a => !a.Optional) && string.IsNullOrEmpty(args)) {
				context.Caller.Respond(CreateHelp());
				return null;
			}

			ReadArgs(context, args);

			foreach (CommandArgument arg in Arguments)
				if (!arg.Optional && !context.ArgumentCache.ContainsKey(arg.Name)) {
					throw new Exception($"No value provided for argument {arg.Name}");
				}

			return MethodCaller!(context);
		}

		public string CreateHelp() {
			StringBuilder builder = new();
			builder.Append(Name);

			foreach (CommandArgument arg in Arguments) {
				builder.Append(' ');
				if (arg.Optional)
					builder.Append("[--");
				builder.Append(arg.Name);
				if (arg.Type != typeof(DBNull)) {
					builder.Append(' ');
					builder.Append(arg.Modifier?.Name ?? arg.Type.Name);
				}
				if (arg.Optional)
					builder.Append(']');
			}

			if (Description is not null) {
				builder.AppendLine();
				builder.AppendLine();
				builder.Append("  ");
				builder.Append(Description);
			}

			foreach (CommandArgument arg in Arguments) {
				if (arg.Description is null && arg.ShortName is null)
					continue;

				builder.AppendLine();
				builder.AppendLine();
				builder.Append("  --");
				builder.Append(arg.Name);
				if (arg.ShortName is not null) {
					builder.Append(", -");
					builder.Append(arg.ShortName.Value);
				}
				if (arg.Type != typeof(DBNull)) {
					builder.Append(" (");
					builder.Append(arg.Modifier?.Name ?? arg.Type.Name);
					builder.Append(')');
				}

				if (arg.Modifier?.Description is not null) {
					builder.AppendLine();
					builder.Append("    ");
					builder.Append(arg.Description);
				}

				if (arg.Description is not null) {
					builder.AppendLine();
					builder.Append("    ");
					builder.Append(arg.Description);
				}
			}

			return builder.ToString();
		}

		void Construct(CommandContext context) {
			if (ExecutionMethod is null)
				throw new Exception($"Command {Name} contains no execution method");

			ConstructMethod(context);

			foreach (CommandArgument arg in Arguments)
				arg.Construct(context);
		}

		void ConstructMethod(CommandContext context) {
			if (MethodCaller is not null)
				return;

			MethodInfo info = ExecutionMethod!.Method;

			List<Expression> args = new();

			ParameterExpression contextParam = Expression.Parameter(typeof(CommandContext), "context");

			ParameterInfo[] @params = info.GetParameters();
			for (int i = 0; i < @params.Length; i++) {
				ParameterInfo param = @params[i];
				if (param.ParameterType == typeof(CommandContext)) {
					args.Add(contextParam);
					continue;
				}
				else if (param.ParameterType == typeof(Command)) {
					args.Add(Expression.Constant(this));
					continue;
				}

				CommandArgument? arg = Arguments.FirstOrDefault(a => a.Name == param.Name);
				if (arg is null) {
					arg = new(param.Name!, param.ParameterType);
					arg.Optional = param.DefaultValue is not DBNull;
					Arguments.Insert(i, arg);
				}

				if (arg.Type != param.ParameterType && arg.Modifier is null)
					throw new Exception($"Command argument {arg.Name} type ({arg.Type.Name}) doesn't match method parameter type {param.ParameterType.Name}");

				Expression argName = Expression.Constant(arg.Name);
				Expression cache = Expression.Property(contextParam, nameof(CommandContext.ArgumentCache));

				Expression containsKey = Expression.Call(cache, nameof(Dictionary<string, object>.ContainsKey), null, argName);
				Expression value = Expression.Property(cache, "Item", argName);

				if (arg.Modifier is not null) {
					value = Expression.Call(Expression.Constant(arg.Modifier), nameof(ArgumentValueModifier.Modify), null, contextParam, value, Expression.Constant(param));
				}

				Expression @default = Expression.Constant(param.DefaultValue is DBNull ? null : param.DefaultValue, param.ParameterType);
				args.Add(Expression.Condition(containsKey, Expression.Convert(value, param.ParameterType), @default));
			}
			Expression? target = ExecutionMethod.Target is null ? null : Expression.Constant(ExecutionMethod.Target);
			Expression body = Expression.Call(target, info, args.ToArray());

			if (info.ReturnType == typeof(void)) {
				body = Expression.Block(
					body,
					Expression.Constant(null)
					);
			}

			var lambda = Expression.Lambda<Func<CommandContext, object?>>(body, contextParam);
			MethodCaller = lambda.Compile();
		}

		void ReadArgs(CommandContext context, string args) {
			CommandReader reader = new(args);
			reader.SkipWhitespaces();
			while (reader.HasData) {
				CommandArgument? arg;
				object? value;

				if (reader.TryMatch(LongArgName, out Match match, true)) {
					string argName = match.Groups[1].Value;
					arg = Arguments.FirstOrDefault(a => a.Name.Equals(argName, StringComparison.InvariantCultureIgnoreCase));

					if (arg is null)
						throw new Exception($"No argument named {argName}");

					if (!arg.TryRead(context, reader, false, out value))
						throw new Exception($"Cannot parse argument {argName} value");

					context.ArgumentCache[argName] = value;
					reader.SkipWhitespaces();
					continue;
				}

				if (reader.TryMatch(ShortArgName, out match, true)) {
					string argName = match.Groups[1].Value;

					arg = Arguments.FirstOrDefault(a => a.Name.Equals(argName, StringComparison.InvariantCultureIgnoreCase));

					if (arg is null) {
						foreach (char shortName in argName) {
							arg = Arguments.FirstOrDefault(a => a.ShortName == shortName);
							if (arg is null)
								throw new Exception($"No argument with short name {shortName}");

							if (!arg.TryRead(context, reader, false, out value))
								throw new Exception($"Cannot parse argument {arg.Name} (short name {shortName}) value");

							context.ArgumentCache[arg.Name] = value;
							reader.SkipWhitespaces();
						}
						continue;
					}

					if (!arg.TryRead(context, reader, false, out value))
						throw new Exception($"Cannot parse argument {argName} value");
					context.ArgumentCache[argName] = value;
					reader.SkipWhitespaces();
					continue;
				}

				arg = Arguments.FirstOrDefault(arg => !context.ArgumentCache.ContainsKey(arg.Name));

				if (arg is null)
					throw new Exception("Positional arguments outside range");

				if (!arg.TryRead(context, reader, false, out value))
					throw new Exception($"Cannot parse argument {arg.Name} value");
				context.ArgumentCache[arg.Name] = value;
				reader.SkipWhitespaces();
			}
		}
	}
}
