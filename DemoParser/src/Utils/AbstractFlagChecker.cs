using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace DemoParser.Utils {
	
	/*
	 * It's one thing when different games have different lists of enums, it's another when those enums are used as
	 * flags. This provides a way to create an object which which only needs to implement the 'HasFlag()' function
	 * and then it can be used to determine if a specific flag exists from an int or for converting the int to a string.
	 * Since the objects are demo specific, they should be stored in the DemoSettings class and preferably created in
	 * the constructor.
	 */
	public abstract class AbstractFlagChecker<T> where T : Enum {

		private static readonly T[] Flags = (T[])Enum.GetValues(typeof(T));
		private static readonly string[] FlagNames = Enum.GetNames(typeof(T));
		private static readonly int MaxStrLen = Enum.GetNames(typeof(T)).Select(s => s.Length + 2).Sum() - 2;


		public abstract bool HasFlag(long val, T flag);


		public bool HasFlags(long val, T flag1, T flag2) {
			return HasFlag(val, flag1) || HasFlag(val, flag2);
		}


		public bool HasFlags(long val, params T[] flags) {
			return flags.Any(f => HasFlag(val, f));
		}


		public string ToFlagString(long val) {
			Span<char> str = stackalloc char[MaxStrLen];
			int len = 0;
			int flagCount = 0;
			for (int i = 0; i < Flags.Length; i++) {
				if (HasFlag(val, Flags[i])) {
					if (flagCount++ > 0) {
						", ".AsSpan().CopyTo(str.Slice(len));
						len += 2;
					}
					FlagNames[i].AsSpan().CopyTo(str.Slice(len));
					len += FlagNames[i].Length;
				}
			}
			return len == 0 ? "None" : str.Slice(0, len).ToString();
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static bool CheckBit(long val, int bit) {
			return (val & ((long)1 << bit)) != 0;
		}
	}
}