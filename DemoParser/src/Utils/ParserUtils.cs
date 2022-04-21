using System;
using System.Collections.Generic;
using System.Linq;
using DemoParser.Parser;

namespace DemoParser.Utils {

	public static class ParserUtils {

		public static int HighestBitIndex(uint i) {
			int j;
			for (j = 31; j >= 0 && (i & (1 << j)) == 0; j--);
			return j;
		}


		// given a dictionary of enum T at index i, returns a dictionary of <i,T> pairs, ignoring any T that matches the exclude key
		internal static Dictionary<T, int> CreateReverseLookupDict<T>(
			this IEnumerable<T> lookup,
			T? excludeKey = null)
			where T : struct, Enum
		{
			var indexed = lookup.Select((@enum,  index) => (index, @enum));
			if (excludeKey != null)
				indexed = indexed.Where(t => !Equals(excludeKey, t.@enum));
			return indexed.ToDictionary(t => t.@enum, t => t.index);
		}
	}


	public readonly struct EHandle { // todo ref to entities

		public readonly uint Val;

		public static implicit operator uint(EHandle h) => h.Val;
		public static explicit operator EHandle(uint u) => new EHandle(u);
		public static explicit operator EHandle(int i) => new EHandle((uint)i);

		public EHandle(uint val) {
			Val = val;
		}

		public int EntIndex => (int)(Val & ((1 << DemoInfo.MaxEdictBits) - 1));
		public int Serial => (int)(Val >> DemoInfo.MaxEdictBits);

		public override string ToString() {
			return Val == DemoInfo.NullEHandle ? "{null}" : $"{{ent index: {EntIndex}, serial: {Serial}}}";
		}
	}
}
