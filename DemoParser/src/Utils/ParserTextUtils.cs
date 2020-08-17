using System;
using System.Collections;
using System.Collections.Generic;
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
			return Convert.ToString(b, 2).PadLeft(8, '0').Substring(0, bitCount);
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
			foreach (object x in enumerable) {
				builder.Append($"{x}{separator}");
				containsElements = true;
			}
			if (containsElements) 
				builder.Remove(builder.Length - separator.Length, separator.Length);
			builder.Append(end);
			return builder.ToString();
		}


		public static string SharedPathSubstring(string s1, string s2) {
			int furthestSlash = 0;
			for (int i = 0; i < s1.Length && i < s2.Length && s1[i] == s2[i]; i++) {
				if (s1[i] == '/' || s1[i] == '\\')
					furthestSlash = i;
			}
			return s1.Substring(0, furthestSlash);
		}
		


		public static void ConsoleWriteWithColor(string s, ConsoleColor color) {
			var original = Console.ForegroundColor;
			Console.ForegroundColor = color;
			Console.Write(s);
			Console.ForegroundColor = original;
		}
	}
}