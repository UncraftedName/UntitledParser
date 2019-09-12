using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static UncraftedDemoParser.Parser.Components.Packets.Packet;

namespace UncraftedDemoParser.Utils {
	
	public static class Extensions {

		public static T[] SubArray<T>(this T[] data, int index, int length) {
			T[] result = new T[length];
			Array.Copy(data, index, result, 0, length);
			return result;
		}


		public static T RequireNonNull<T>(this T o) {
			if (o == null)
				throw new NullReferenceException("something is null that isn't supposed to be");
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
	}
}