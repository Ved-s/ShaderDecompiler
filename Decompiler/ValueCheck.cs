namespace ShaderDecompiler.Decompiler {
	public struct ValueCheck<T> {
		readonly Func<T?, bool>? CheckDelegate;
		readonly T? ConstantValue;
		bool ConstantCheck = false;
		bool AnyCheck;

		public static ValueCheck<T> Any => new(true);
		public static ValueCheck<T> None => new(false);

		public ValueCheck(bool constantResult) {
			AnyCheck = constantResult;
		}

		public ValueCheck(T? constantValue) {
			ConstantValue = constantValue;
			ConstantCheck = true;
		}

		public ValueCheck(Func<T?, bool> checkDelegate) {
			CheckDelegate = checkDelegate;
		}

		public bool Check(T? value) {
			if (CheckDelegate is not null)
				return CheckDelegate(value);

			if (ConstantCheck)
				return Equals(value, ConstantValue);

			return AnyCheck;
		}

		public static implicit operator ValueCheck<T>(Func<T?, bool> checkDelegate) => new(checkDelegate);
		public static implicit operator ValueCheck<T>(T? value) => new(value);
		public static implicit operator ValueCheck<T>(bool constantResult) => new(constantResult);
	}
}
