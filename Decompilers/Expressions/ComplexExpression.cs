﻿using ShaderDecompiler.Structures;
using System.Diagnostics;

namespace ShaderDecompiler.Decompilers.Expressions {
	public abstract class ComplexExpression : Expression {
		public Expression[] SubExpressions = Array.Empty<Expression>();
		public abstract ValueCheck<int> ArgumentCount { get; }

		public static T Create<T>(params Expression[] expressions) where T : ComplexExpression, new() {
			if (typeof(T) == typeof(AssignExpression) && expressions[0] is not RegisterExpression)
				Debugger.Break();

			T expr = new();
			if (!expr.ArgumentCount.Check(expressions.Length))
				throw new ArgumentException("Wrong parameter count", nameof(expressions));
			expr.SubExpressions = expressions;
			return expr;
		}
		
		public override SwizzleMask GetRegisterUsage(ParameterRegisterType type, uint index, bool? destination) {
			if (SubExpressions.Length == 0)
				return SwizzleMask.None;

			return SubExpressions.Select(expr => expr.GetRegisterUsage(type, index, destination)).SafeAggregate((a, b) => a | b);
		}

		public sealed override Expression Simplify(ShaderDecompilationContext context, bool allowComplexityIncrease, out bool fail) {
			fail = true;

			for (int i = 0; i < SubExpressions.Length; i++) {
				bool tooComplex = CalculateComplexity() + SubExpressions[i].CalculateComplexity() > context.ComplexityThreshold;

				SubExpressions[i] = SubExpressions[i].Simplify(context, allowComplexityIncrease && !tooComplex, out bool exprFail);
				fail &= exprFail;
			}

			Expression expr = SimplifySelf(context, allowComplexityIncrease, out bool selfFail);
			fail &= selfFail;
			return expr;
		}

		public sealed override Expression Clone() {
			ComplexExpression expr = CloneSelf();
			expr.SubExpressions = new Expression[SubExpressions.Length];
			for (int i = 0; i < expr.SubExpressions.Length; i++)
				expr.SubExpressions[i] = SubExpressions[i].Clone();
			return expr;
		}

		public override int CalculateComplexity() {
			return 1 + SubExpressions.Sum(expr => expr.CalculateComplexity());
		}

		public override void MaskSwizzle(SwizzleMask mask) {
			for (int i = 0; i < SubExpressions.Length; i++)
				SubExpressions[i].MaskSwizzle(ModifySubSwizzleMask(mask, i));
		}

		public virtual ComplexExpression CloneSelf() => (ComplexExpression)Activator.CreateInstance(GetType())!;

		public virtual Expression SimplifySelf(ShaderDecompilationContext context, bool allowComplexityIncrease, out bool fail) {
			fail = true;
			return this;
		}

		public virtual SwizzleMask ModifySubSwizzleMask(SwizzleMask mask, int subIndex) => mask;
	}
}