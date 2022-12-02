namespace ShaderDecompiler.Decompiler.Expressions {
	public class AssignExpression : ComplexExpression {
		public override ValueCheck<int> ArgumentCount => 2;

		public RegisterExpression Destination => SubExpressions[0] as RegisterExpression ?? throw new InvalidDataException();
		public Expression Source => SubExpressions[1];

		public override string Decompile(ShaderDecompilationContext context) {
			bool needsType = context.Scan.Arguments.All(arg => arg.RegisterType != Destination.Type || arg.Register != Destination.Index)
				&& (context.CurrentExpressionIndex == 0 || context.Expressions
					.Take(context.CurrentExpressionIndex)
					.All(expr => expr is not AssignExpression assign || !assign.Destination.IsSameRegisterAs(Destination)));

			string type = "";
			if (needsType) {
				if (!context.Scan.RegisterSizes.TryGetValue((Destination.Type, Destination.Index), out uint size))
					size = 4;

				if (!Destination.FullRegister) {
					if (size > 1)
						type = $"float{size} {Destination.GetName(context)};\n";
					else
						type = $"float {Destination.GetName(context)};\n";
				}
				else {
					if (size > 1)
						type = $"float{size} ";
					else
						type = "float ";
				}
			}

			return $"{type}{Destination.Decompile(context)} = {Source.Decompile(context)}";
		}

		public override bool Clean(ShaderDecompilationContext context) {
			if (!context.Scan.RegistersReferenced.Contains((Destination.Type, Destination.Index, false))) {
				// Don't remove registers that weren't used in the first place
				return false;
			}

			if (context.Scan.Arguments.Any(arg => arg.Output && arg.RegisterType == Destination.Type && arg.Register == Destination.Index))
				return false;

			SwizzleMask destMask = Destination.UsageMask;

			for (int i = context.CurrentExpressionIndex + 1; i < context.Expressions.Count; i++) {
				if (context.Expressions[i] is null)
					continue;

				if (i != context.CurrentExpressionIndex) {
					SwizzleMask usage = context.Expressions[i]!.GetRegisterUsage(Destination.Type, Destination.Index, false);
					if ((usage & destMask) != SwizzleMask.None)
						return false;
				}

				if (context.Expressions[i] is AssignExpression assign 
			     && assign.Destination.IsSameRegisterAs(Destination)
				 && (assign.Destination.UsageMask & destMask) == destMask) // If assign.Destination fully overrides current destination
					break;
			}
			return true;
		}

		public override string ToString() {
			return $"{Destination} = {Source}";
		}
	}
}
