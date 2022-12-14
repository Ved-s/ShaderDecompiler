#region License
/*
  ShaderDecompiler - Direct3D shader decompiler

  Released under Microsoft Public License
  See LICENSE for details
*/
#endregion

using ShaderDecompiler.Structures;
using System.Diagnostics;

namespace ShaderDecompiler.Decompilers.Expressions {
	public class RegisterExpression : Expression {
		public ParameterRegisterType Type;
		public uint Index;
		public Swizzle? X;
		public Swizzle? Y;
		public Swizzle? Z;
		public Swizzle? W;
		public bool Destination;

		public bool FullRegister => X == Swizzle.X && Y == Swizzle.Y && Z == Swizzle.Z && W == Swizzle.W;
		public SwizzleMask WriteMask {
			get {
				if (!Destination)
					throw new InvalidOperationException("Register is not destination");

				SwizzleMask mask = SwizzleMask.None;

				if (X.HasValue) mask |= SwizzleMask.X;
				if (Y.HasValue) mask |= SwizzleMask.Y;
				if (Z.HasValue) mask |= SwizzleMask.Z;
				if (W.HasValue) mask |= SwizzleMask.W;

				return mask;
			}
		}

		public SwizzleMask UsageMask => EnumerateSwizzles().Select(sw => sw.ToMask()).SafeAggregate((a, b) => a | b);

		public bool Ordered {
			get {
				Swizzle current = Swizzle.X;
				foreach (Swizzle sw in EnumerateSwizzles()) {
					if (sw != current)
						return false;
					current++;
				}
				return true;
			}
		}
		public bool Single {
			get {
				Swizzle? swizzle = null;
				foreach (Swizzle sw in EnumerateSwizzles()) {
					if (swizzle is null)
						swizzle = sw;
					else if (swizzle.Value != sw)
						return false;
				}
				return true;
				
			}
		}

		public RegisterExpression(ParameterRegisterType type, uint index, Swizzle? x, Swizzle? y, Swizzle? z, Swizzle? w, bool destination) {
			Type = type;
			Index = index;
			X = x;
			Y = y;
			Z = z;
			W = w;
			Destination = destination;
		}

		public override SwizzleMask GetRegisterUsage(ParameterRegisterType type, uint index, bool? destination) {
			if (type == Type && index == Index && (destination is null || destination == Destination))
				return UsageMask;

			return SwizzleMask.None;
		}

		public override IEnumerable<RegisterExpression> EnumerateRegisters() {
			yield return this;
		}

		public override string Decompile(ShaderDecompilationContext context) {

			bool full = FullRegister;

			if ((Ordered || Single) && context.Scan.RegisterSizes.TryGetValue((Type, Index), out uint size) && ((int)UsageMask).Bits() == size)
				full = true;

			if (full)
				return GetName(context);

			if (X.HasValue && Y.HasValue && Z.HasValue && W.HasValue && X.Value == Y.Value && Y.Value == Z.Value && Z.Value == W.Value)
			    return $"{GetName(context)}.{X?.ToString().ToLower()}";

			return $"{GetName(context)}.{X?.ToString().ToLower()}{Y?.ToString().ToLower()}{Z?.ToString().ToLower()}{W?.ToString().ToLower()}";
		}

		public string GetName(ShaderDecompilationContext context) {
			if (!context.RegisterNames.TryGetValue((Type, Index), out string? name))
				if (SourceParameter.RegisterTypeNames.TryGetValue(Type, out name))
					name = $"{name}{Index}";
				else
					name = $"{Type.ToString().ToLower()}{Index}";
			return name;
		}

		public bool IsSameRegisterAs(RegisterExpression expr)
			=> expr.Type == Type
			&& expr.Index == Index;

		public bool IsExactRegisterAs(RegisterExpression expr) {
			if (!IsSameRegisterAs(expr))
				return false;

			return UsageMask == expr.UsageMask;
		}

		public IEnumerable<Swizzle> EnumerateSwizzles() {
			if (X.HasValue) yield return X.Value;
			if (Y.HasValue) yield return Y.Value;
			if (Z.HasValue) yield return Z.Value;
			if (W.HasValue) yield return W.Value;
		}

		public override Expression Simplify(ShaderDecompilationContext context, bool allowComplexityIncrease, out bool fail) {
			fail = true;

			if (Destination)
				return this;

			if (Type == ParameterRegisterType.PreshaderLiteral 
			 && context.Shader.Preshader is Preshader pres 
			 && pres.Literals is not null 
			 && Index < pres.Literals.Length) {
				fail = false;
				return new ConstantExpression((float)pres.Literals[Index]);
			}

			if (!allowComplexityIncrease)
				return this;

			
			SwizzleMask thisMask = UsageMask;

			if (Type != ParameterRegisterType.Const || !context.Scan.DeclaredConstants.Contains(Index)) {

				// If this register is used inbetween this expression and next assignment (including current expression) to the register or end
				SwizzleMask accumulatedMask = thisMask;
				for (int i = context.CurrentExpressionIndex; i < context.Expressions.Count; i++) {
					if (context.Expressions[i] is null)
						continue;

					if (i != context.CurrentExpressionIndex) {
						SwizzleMask usage = context.Expressions[i]!.GetRegisterUsage(Type, Index, false);
						if ((usage & thisMask) != SwizzleMask.None) // If any channels used here are used elsewhere
							return this;
					}

					accumulatedMask ^= (accumulatedMask & context.Expressions[i]!.GetRegisterUsage(Type, Index, true));

					if (accumulatedMask == SwizzleMask.None) // If register is fully overridden
						break;

					else if (context.Expressions[i]!.EnumerateRegisters().Count(reg => reg.Type == Type && reg.Index == Index && reg.Destination == Destination) > 1)
						return this;
				}

				// If this register is used inbetween this expression and prevoius assignment (excluding current expression) to the register or end

				if (context.CurrentExpressionIndex > 0) {
					for (int i = context.CurrentExpressionIndex - 1; i >= 0; i--) {
						if (context.Expressions[i] is null)
							continue;

						SwizzleMask write = context.Expressions[i]!.GetRegisterUsage(Type, Index, true);
						if (write == thisMask) // Register is fully written
							break;

						if ((write & thisMask) != SwizzleMask.None) // Register is partially written before
							return this;

						// Don't try to optimize if this register's channels were read before
						if (i != context.CurrentExpressionIndex) {
							SwizzleMask usage = context.Expressions[i]!.GetRegisterUsage(Type, Index, false);
							if ((usage & thisMask) != SwizzleMask.None)
								return this;
						}

					}
				}
			}

			Expression? assignment = null;

			// Search for matching replacement
			if (context.CurrentExpressionIndex > 0) {
				for (int i = context.CurrentExpressionIndex - 1; i >= 0; i--) {
					if (context.Expressions[i] is null)
						continue;

					if (context.Expressions[i] is AssignExpression assign) {
						if (assign.Destination.IsSameRegisterAs(this)) {
							if (assign.Source is ValueCtorExpression ctor) {
								if ((assign.Destination.WriteMask & thisMask) == thisMask) {
									List<Expression> values = new();

									foreach (Swizzle sw in EnumerateSwizzles())
										values.Add(ctor.SubExpressions[(int)sw]);

									if (values.Count > 0) {
										fail = false;

										if (values.Count == 1)
											return values[0];

										if (values.All(v => v is ConstantExpression)) {
											float check = ((ConstantExpression)values[0]).Value;
											if (values.Skip(1).All(v => v is ConstantExpression @const && @const.Value == check))
												return values[0];
										}

										return ComplexExpression.Create<ValueCtorExpression>(values.ToArray());
									}
								}
							}

							if (assign.Destination.IsExactRegisterAs(this)) {
								if (CheckWeightExceededWith(context, assign.Source))
									return this;

								context.Expressions[i] = null;
								assignment = assign.Source;
								break;
							}
						}
					}
				}
			}

			if (assignment is not null) {
				fail = false;
				return assignment.Clone();
			}
			return this;
		}

		public override Expression Clone() {
			return new RegisterExpression(Type, Index, X, Y, Z, W, Destination);
		}

		public override void MaskSwizzle(SwizzleMask mask) {
			if (!mask.HasFlag(SwizzleMask.X)) X = null;
			if (!mask.HasFlag(SwizzleMask.Y)) Y = null;
			if (!mask.HasFlag(SwizzleMask.Z)) Z = null;
			if (!mask.HasFlag(SwizzleMask.W)) W = null;
		}

		public override string ToString() {
			return $"{Type.ToString().ToLower()}{Index}.{X?.ToString().ToLower() ?? "_"}{Y?.ToString().ToLower() ?? "_"}{Z?.ToString().ToLower() ?? "_"}{W?.ToString().ToLower() ?? "_"}";
		}

		bool CheckWeightExceededWith(ShaderDecompilationContext context, Expression expr) {
			return context.Expressions[context.CurrentExpressionIndex]!.CalculateComplexity() - CalculateComplexity() + expr.CalculateComplexity() > context.Settings.ComplexityThreshold;
		}
	}
}
