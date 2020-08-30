using System.Reflection;

// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices {
	/*
	 * Polyfill for RuntimeHelpers.GetSubArray to allow range-based indexing of arrays.
	 * May break other obscure language features that rely on RuntimeHelpers, hence it's
	 * only internal (ConsoleApp actually needs the real RuntimeHelpers because PathExt
	 * uses some unsafe string code, lol).
	 */
	internal static class RuntimeHelpers {
		public static T[] GetSubArray<T>(T[] array, Range range) {
			if (array == null) throw new ArgumentNullException();
			int start = range.Start.GetOffset(array.Length);
			int end = range.End.GetOffset(array.Length);
			int len = end - start + 1;

			T[] dest;
			if (default(T)! != null || typeof(T[]) == array.GetType()) {
				if (len == 0) return Array.Empty<T>();
				dest = new T[len];
			}
			else {
				dest = (T[])Array.CreateInstance(array.GetType().GetElementType()!, len);
			}
			Array.Copy(array, start, dest, 0, len);
			return dest;
		}

		private static readonly MethodInfo RealGetHashCode;

		static RuntimeHelpers() {
			var t = Type.GetType("System.Runtime.CompilerServices.RuntimeHelpers, mscorlib");
			RealGetHashCode = t!.GetMethod("GetHashCode", new[] {typeof(object)});
		}
		

		/* Have to do some sad wrapping here because otherwise this function is simply gone */
		public static int GetHashCode(object o) {
			return (int)RealGetHashCode.Invoke(null, new[] {o});
		}
	}
}
