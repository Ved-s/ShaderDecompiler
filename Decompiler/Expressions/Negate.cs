﻿namespace ShaderDecompiler.Decompiler.Expressions {
	public class NegateExpression : ComplexExpression {
		public Expression Expression => SubExpressions[0];
		public override ValueCheck<int> ArgumentCount => 1;

		public override string Decompile(ShaderDecompilationContext context) {
			bool needsWrap = Expression is ComplexExpression and not CallExpression;
			if (needsWrap)
				return $"-({Expression.Decompile(context)})";
			return $"-{Expression.Decompile(context)}";
		}

		public override string ToString() {
			bool needsWrap = Expression is ComplexExpression and not CallExpression;
			if (needsWrap)
				return $"-({Expression})";
			return $"-{Expression}";
		}
	}
}
