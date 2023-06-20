/*
 * More-or-less ripped straight from .NET Core runtime, with some ws/comments stripped out
 *
 * Licensed to the .NET Foundation under one or more agreements.
 * The .NET Foundation licenses this file to you under the MIT license.
 */

// FIXME: This is very Windows-specific! This will probably be one of the things
// that needs fixed up if/when doing a Linux/Mono port.

using System;
using System.IO;
using System.Text;

namespace ConsoleApp {

	internal static class PathExt {

		private static bool IsDirectorySeparator(char c) => c == '\\' || c == '/';
		private static bool EndsInDirectorySeparator(string path)
			=> path.Length > 0 && IsDirectorySeparator(path[^1]);

		private static unsafe int EqualStartingCharacterCount(string? first, string? second, bool ignoreCase) {
			if (string.IsNullOrEmpty(first) || string.IsNullOrEmpty(second)) return 0;

			int commonChars = 0;
			fixed (char* f = first)
			fixed (char* s = second) {
				char* l = f;
				char* r = s;
				char* leftEnd = l + first!.Length;
				char* rightEnd = r + second!.Length;

				while (l != leftEnd && r != rightEnd
					&& (*l == *r || (ignoreCase && char.ToUpperInvariant(*l) == char.ToUpperInvariant(*r)))) {
					commonChars++;
					l++;
					r++;
				}
			}

			return commonChars;
		}

		private static int GetCommonPathLength(string first, string second, bool ignoreCase) {
			int commonChars = EqualStartingCharacterCount(first, second, ignoreCase);
			if (commonChars == 0) return commonChars;
			if (commonChars == first.Length && (commonChars == second.Length ||
					IsDirectorySeparator(second[commonChars])))
				return commonChars;
			if (commonChars == second.Length && IsDirectorySeparator(first[commonChars]))
				return commonChars;
			while (commonChars > 0 && !IsDirectorySeparator(first[commonChars - 1]))
				commonChars--;
			return commonChars;
		}

		private static bool AreRootsEqual(string first, string second, StringComparison comparisonType) {
			int firstRootLength = GetRootLength(first);
			int secondRootLength = GetRootLength(second);

			return firstRootLength == secondRootLength
				&& string.Compare(
					strA: first,
					indexA: 0,
					strB: second,
					indexB: 0,
					length: firstRootLength,
					comparisonType: comparisonType) == 0;
		}

		private const int
			DevicePrefixLength = 4,
			UncPrefixLength = 2,
			UncExtendedPrefixLength = 8;

		private static bool IsExtended(string path) {
			return path.Length >= DevicePrefixLength
				&& path[0] == '\\'
				&& (path[1] == '\\' || path[1] == '?')
				&& path[2] == '?'
				&& path[3] == '\\';
		}

		private static bool IsDevice(string path) {
			return IsExtended(path)
				||
				(
					path.Length >= DevicePrefixLength
					&& IsDirectorySeparator(path[0])
					&& IsDirectorySeparator(path[1])
					&& (path[2] == '.' || path[2] == '?')
					&& IsDirectorySeparator(path[3])
				);
		}

		private static bool IsDeviceUNC(string path) {
			return path.Length >= UncExtendedPrefixLength
				&& IsDevice(path)
				&& IsDirectorySeparator(path[7])
				&& path[4] == 'U'
				&& path[5] == 'N'
				&& path[6] == 'C';
		}

		private static bool IsValidDriveChar(char value) {
			return (value >= 'A' && value <= 'Z') || (value >= 'a' && value <= 'z');
		}

		private static int GetRootLength(string path) {
			int pathLength = path.Length;
			int i = 0;

			bool deviceSyntax = IsDevice(path);
			bool deviceUnc = deviceSyntax && IsDeviceUNC(path);
			if ((!deviceSyntax || deviceUnc) && pathLength > 0 && IsDirectorySeparator(path[0])) {
				if (deviceUnc || (pathLength > 1 && IsDirectorySeparator(path[1]))) {
					i = deviceUnc ? UncExtendedPrefixLength : UncPrefixLength;
					int n = 2;
					while (i < pathLength && (!IsDirectorySeparator(path[i]) || --n > 0)) i++;
				} else {
					i = 1;
				}
			} else if (deviceSyntax) {
				i = DevicePrefixLength;
				while (i < pathLength && !IsDirectorySeparator(path[i])) i++;
				if (i < pathLength && i > DevicePrefixLength && IsDirectorySeparator(path[i])) i++;
			}
			else if (pathLength >= 2
					&& path[1] == ':'
					&& IsValidDriveChar(path[0])) {
				i = 2;
				if (pathLength > 2 && IsDirectorySeparator(path[2])) i++;
			}
			return i;
		}

		public static string GetRelativePath(string relativeTo, string path) {

			if (!Utils.IsWindows())
				return Path.GetRelativePath(relativeTo, path);

			// just windows things
			const StringComparison comparisonType = StringComparison.OrdinalIgnoreCase;

			if (relativeTo == null) throw new ArgumentNullException(nameof(relativeTo));
			if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Empty path", nameof(relativeTo));
			if (path == null) throw new ArgumentNullException(nameof(path));
			if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Empty path", nameof(path));
			relativeTo = Path.GetFullPath(relativeTo);
			path = Path.GetFullPath(path);
			if (!AreRootsEqual(relativeTo, path, comparisonType)) return path;
			int commonLength = GetCommonPathLength(relativeTo, path, ignoreCase: comparisonType == StringComparison.OrdinalIgnoreCase);
			if (commonLength == 0) return path;
			int relativeToLength = relativeTo.Length;
			if (EndsInDirectorySeparator(relativeTo)) relativeToLength--;
			bool pathEndsInSeparator = EndsInDirectorySeparator(path);
			int pathLength = path.Length;
			if (pathEndsInSeparator) pathLength--;
			if (relativeToLength == pathLength && commonLength >= relativeToLength) return ".";
			var sb = new StringBuilder();
			sb.EnsureCapacity(Math.Max(relativeTo.Length, path.Length));
			if (commonLength < relativeToLength) {
				sb.Append("..");
				for (int i = commonLength + 1; i < relativeToLength; i++) {
					if (IsDirectorySeparator(relativeTo[i])) {
						sb.Append("\\..");
					}
				}
			}
			else if (IsDirectorySeparator(path[commonLength])) {
				commonLength++;
			}
			int differenceLength = pathLength - commonLength;
			if (pathEndsInSeparator) differenceLength++;
			if (differenceLength > 0) {
				if (sb.Length > 0) sb.Append(Path.DirectorySeparatorChar);
				sb.Append(path.AsSpan(commonLength, differenceLength).ToString());
			}

			return sb.ToString();
		}
	}
}
