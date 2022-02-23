using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace DemoParser.Utils {

	public static class ParserTextUtils {

		public static string BytesToBinaryString(IEnumerable<byte> bytes) {
			return string.Join(" ", bytes
				.Select(b => Convert.ToString(b, 2)).ToList()
				.Select(s => s.PadLeft(8, '0')));
		}


		public static string ByteToBinaryString(byte b, int bitCount = 8) {
			return Convert.ToString(b, 2).PadLeft(8, '0')[(8 - bitCount)..];
		}


		public static string BytesToHexString(IEnumerable<byte> bytes, string separator = " ") {
			return string.Join(separator, bytes
				.Select(b => Convert.ToString(b, 16)).ToList()
				.Select(s => s.PadLeft(2, '0')));
		}


		// https://stackoverflow.com/questions/18781027/regex-camel-case-to-underscore-ignore-first-occurrence
		public static string CamelCaseToUnderscore(string str) {
			return string.Concat(str.Select((x,i) => i > 0 && char.IsUpper(x) ? "_" + x : x.ToString())).ToUpper();
		}


		public static byte[] StringAsByteArray(string s, int length) { // fills the unused indices with '\0'
			byte[] result = new byte[length];
			Array.Copy(Encoding.ASCII.GetBytes(s), result, s.Length);
			return result;
		}


		public static unsafe string ByteArrayAsString(byte[] bytes) { // cuts off all '\0' from the end of the array
			fixed (byte* bytePtr = bytes)
				return new string((sbyte*)bytePtr);
		}


		public static string SequenceToString(this IEnumerable enumerable, string separator = ", ", string start = "[", string end = "]") {
			StringBuilder builder = new StringBuilder(start);
			bool containsElements = false;
			foreach (object? x in enumerable) {
				builder.Append($"{x}{separator}");
				containsElements = true;
			}
			if (containsElements)
				builder.Remove(builder.Length - separator.Length, separator.Length);
			builder.Append(end);
			return builder.ToString();
		}


		// https://stackoverflow.com/questions/323640/can-i-convert-a-c-sharp-string-value-to-an-escaped-string-literal
		// escapes stuff like tabs & new lines for pretty printing
		public static string ToLiteral(this string str) {
			// it's lit fam
			StringBuilder lit = new StringBuilder(str.Length + 2);
			lit.Append("\"");
			foreach (char c in str) {
				switch (c) {
					case '\"': lit.Append("\\\""); break;
					case '\\': lit.Append(@"\\");  break;
					case '\0': lit.Append("\\0");  break;
					case '\a': lit.Append("\\a");  break;
					case '\b': lit.Append("\\b");  break;
					case '\f': lit.Append("\\f");  break;
					case '\n': lit.Append("\\n");  break;
					case '\r': lit.Append("\\r");  break;
					case '\t': lit.Append("\\t");  break;
					case '\v': lit.Append("\\v");  break;
					default:
						if (char.GetUnicodeCategory(c) != UnicodeCategory.Control) {
							lit.Append(c);
						} else {
							lit.Append(@"\u");
							lit.Append(((ushort)c).ToString("x4"));
						}
						break;
				}
			}
			lit.Append("\"");
			return lit.ToString();
		}
	}
}
