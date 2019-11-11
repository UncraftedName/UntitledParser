using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using static UncraftedDemoParser.Parser.Components.Packets.Packet;

namespace UncraftedDemoParser.Utils {
	
	public static class Extensions {

		public static T[] SubArray<T>(this T[] data, int index, int length, [CallerMemberName] string caller = "") {
			T[] result = new T[length];
			if (index + length > data.Length) {
				Array.Copy(data, index, result, 0, data.Length - index);
				Debug.WriteLine(
					$"SubArray() trying to go out of bounds, writing {(data.Length - index)} bytes instead of {length}; caller: {caller}");
			} else {
				Array.Copy(data, index, result, 0, length);
			}

			return result;
		}


		public static T RequireNonNull<T>(this T o) {
			if (o == null)
				throw new NullReferenceException("something is null :thinking:");
			return o;
		}


		// removes count items that match the given predicate after or before the given index depending on backwardsSearch
		public static void RemoveItemsAfterIndex<T>(this List<T> list, Predicate<T> predicate, int index = 0, int count = 1, bool backwardsSearch = false) {
			if (backwardsSearch) {
				for (int i = index - 1; i >= 0 && count > 0; i--) {
					if (predicate.Invoke(list[i])) {
						list.RemoveAt(i);
						count--;
					}
				}
			} else {
				for (int i = index + 1; i < list.Count && count > 0; i++) {
					if (predicate.Invoke(list[i])) {
						list.RemoveAt(i);
						count--;
						i--;
					}
				}
			}
		}


		// convert byte array to binary as a string
		public static string AsBinStr(this byte[] bytes) {
			return String.Join(" ", 
				bytes.ToList()
					.Select(b => Convert.ToString(b, 2)).ToList()
					.Select(s => s.PadLeft(8, '0')));
		}


		public static string AsHexStr(this byte[] bytes) {
			return String.Join(" ",
				bytes.ToList()
					.Select(b => Convert.ToString(b, 16)).ToList()
					.Select(s => s.PadLeft(2, '0')));
		}


		public static void WriteToFiles(this string data, params string[] files) {
			files.ToList().ForEach(file => File.WriteAllText(file, data));
		}


		public static T[] AppendedWith<T>(this T[] arr1, T[] arr2) {
			T[] output = new T[arr1.Length + arr2.Length];
			arr1.CopyTo(output, 0);
			arr2.CopyTo(output, arr1.Length);
			return output;
		}


		public static T[] AppendedWith<T>(this T[] arr, T item) {
			T[] output = new T[arr.Length + 1];
			arr.CopyTo(output, 0);
			arr[arr.Length - 1] = item;
			return output;
		}


		public static string QuickToString<T>(this IEnumerable<T> enumerable, string separator = ", ", string start = "[", string end = "]") {
			StringBuilder builder = new StringBuilder(start);
			bool containsElements = false;
			foreach (T x in enumerable) {
				builder.Append($"{x}{separator}");
				containsElements = true;
			}
			if (containsElements) 
				builder.Remove(builder.Length - separator.Length, separator.Length);
			builder.Append(end);
			return builder.ToString();
		}


		public static string GetPathRelativeTo(this string relativeTo, string path) {
			//Console.WriteLine();
			return Uri.UnescapeDataString(new Uri(Path.GetFullPath(path)).MakeRelativeUri(new Uri(Path.GetFullPath(relativeTo))).ToString());
		}
	}
}