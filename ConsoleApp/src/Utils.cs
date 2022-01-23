using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using DemoParser.Utils;

namespace ConsoleApp {

	public static class Utils {

		private static readonly Stack<ConsoleColor> ForegroundColors = new Stack<ConsoleColor>();


		public static void PushForegroundColor() => PushForegroundColor(Console.ForegroundColor);

		public static void PushForegroundColor(ConsoleColor color) {
			ForegroundColors.Push(Console.ForegroundColor);
			Console.ForegroundColor = color;
		}


		public static ConsoleColor PopForegroundColor() {
			ConsoleColor c = ForegroundColors.Pop();
			Console.ForegroundColor = c;
			return c;
		}


		public static void WriteColor(string s, ConsoleColor color) {
			PushForegroundColor(color);
			Console.Write(s);
			PopForegroundColor();
		}


		public static void Warning(string s) => WriteColor(s, ConsoleColor.Red);


		// gets the common shared path between s1 & s2, e.g. basement/creepy/cats, basement/friendly/cats -> basement
		public static string SharedPathSubstring(string s1, string s2) {
			int furthestSlash = 0;
			for (int i = 0; i < s1.Length && i < s2.Length && s1[i] == s2[i]; i++)
				if (s1[i] == '/' || s1[i] == '\\')
					furthestSlash = i;
			return s1.Substring(0, furthestSlash);
		}


		public static bool HasFlagsAttribute(Type type) {
			Debug.Assert(type.IsEnum);
			return type.IsDefined(typeof(FlagsAttribute), false);
		}


		// Ah yes I love swimming through strongly typed enum garbage, my favorite.
		// Assumes that enum values are all positive and don't skip any values (and powers of 2 if using flags).
		public static T ParseEnum<T>(string arg) where T : Enum {
			arg = arg.Trim().ToLower();
			if (HasFlagsAttribute(typeof(T)) && (arg == "" || arg.Equals("none", StringComparison.OrdinalIgnoreCase)))
				return default!;
			try {
				ulong val;
				if (HasFlagsAttribute(typeof(T))) {
					// I want to support 'enum1|enum2' in addition to 'enum1, enum2'. Add up all values and remove
					// duplicates so you can't do 2|2=4.
					val =
						(ulong)Regex.Split(arg, @"\||\s*,\s*")
							.Select(s => s.Trim())
							.Where(s => s.Length > 0)
							.Distinct()
							.Select(s => Convert.ToInt64(Enum.Parse(typeof(T), s, true)))
							.Sum(); // why the frick does Sum() not support ulong?
				} else {
					val = Convert.ToUInt64(Enum.Parse(typeof(T), arg, true));
				}
				var enumVals = Enum.GetValues(typeof(T)).Cast<object>().Select(Convert.ToInt64);
				// Check if the value is outside the range of the enum.
				ulong maxPossible = HasFlagsAttribute(typeof(T)) ? (ulong)enumVals.Sum() : enumVals.Select(l => (ulong)l).Max();
				if (val > maxPossible)
					throw new Exception(); // I don't think I can even use goto here so just throw and catch immediately
				return (T)Enum.ToObject(typeof(T), val);
			} catch (Exception) {
				throw new ArgProcessUserException("could not convert to valid enum value");
			}
		}


		public static string FormatTime(double seconds) {
			if (Math.Abs(seconds) > 8640000) // 10 days, probably happens from initializing the tick interval to garbage
				return "invalid";
			string sign = seconds < 0 ? "-" : "";
			// timespan truncates values, (makes sense since 0.5 minutes is still 0 total minutes) so I round manually
			TimeSpan t = TimeSpan.FromSeconds(Math.Abs(seconds));
			int roundedMilli = t.Milliseconds + (int)Math.Round(t.TotalMilliseconds - (long)t.TotalMilliseconds);
			if (t.Days > 0)
				return $"{sign}{t.Days:D1}.{t.Hours:D2}:{t.Minutes:D2}:{t.Seconds:D2}.{roundedMilli:D3}";
			else if (t.Hours > 0)
				return $"{sign}{t.Hours:D1}:{t.Minutes:D2}:{t.Seconds:D2}.{roundedMilli:D3}";
			else if (t.Minutes > 0)
				return $"{sign}{t.Minutes:D1}:{t.Seconds:D2}.{roundedMilli:D3}";
			else
				return $"{sign}{t.Seconds:D1}.{roundedMilli:D3}";
		}


		public static string GetExeName() => QuoteIfHasSpaces(AppDomain.CurrentDomain.FriendlyName);


		[System.Runtime.InteropServices.DllImport("kernel32.dll")]
		private static extern int GetConsoleProcessList(int[] buffer, int size);


		// used to check if we should use ReadKey() once we finish
		public static bool WillBeDestroyedOnExit() {
			return !Debugger.IsAttached && GetConsoleProcessList(new int[2], 2) <= 1;
		}


		public static string QuoteIfHasSpaces(string s) => s.Contains(" ") ? $"\"{s}\"" : s;


		// to be frank I have no idea how this assembly stuff works
		public static string? GetVersionString() {
			try {
				var assembly = Assembly.GetExecutingAssembly();
				DateTime? buildDate = BuildDateAttribute.GetBuildDate(Assembly.GetExecutingAssembly());
				if (!buildDate.HasValue)
					return null;
				using Stream stream = assembly.GetManifestResourceStream($"{nameof(ConsoleApp)}.version.txt")!;
				using StreamReader reader = new StreamReader(stream);
				int numCommits = int.Parse(reader.ReadLine()!);
				string branch = reader.ReadLine()!;
				string commitHash = reader.ReadLine()!;
				return $"v.{numCommits} {branch} branch ({commitHash}), {buildDate.Value:yyyy-MM-dd H:mm:ss}";
			} catch (Exception) {
				return null;
			}
		}


		// microsoft, you autocomplete dir to '.\dir name\' and then that last single quote escapes into a double quote which fucks everything
		public static string[] FixPowerShellBullshit(string[] args) {
			// example input: '.\nosla 12 50 89\' '.\nosla 12 50 89\' gets converted to [.\nosla 12 50 89" .\nosla, 12, 50, 89"]
			var newArgs = new List<string>();
			bool fixing = false;
			foreach (string arg in args) {
				if (!fixing && arg.Substring(0, 2) == @".\" && arg.Contains('"')) { // this is a directory that breaks future args
					int off = arg.IndexOf('"');
					newArgs.Add(arg.Substring(0, off));
					if (off < arg.Length - 1) {
						// split the string by spaces, these should be separate args
						newArgs.AddRange(arg.Substring(off + 2).Split(' '));
						fixing = true;
					}
				} else if (fixing) {
					// we're fixing, these separate args are actually the same arg separated by a space
					newArgs[^1] += " ";
					if (arg.Last() == '"') {
						// this is the end of the current arg
						newArgs[^1] += arg.Substring(0, arg.Length - 1);
						fixing = false;
					} else if (arg.Contains(' ')) {
						// this is the end of the current arg and the beginning of the next one(s)
						var splitArgs = arg.Split(' ');
						newArgs[^1] += splitArgs[0];
						newArgs.AddRange(splitArgs.Skip(1));
					} else {
						newArgs[^1] += arg;
					}
				} else {
					newArgs.Add(arg);
				}
			}
			return newArgs.ToArray();
		}
	}


	/// <summary>
	/// Throw this when doing console parsing if the user made a mistake.
	/// </summary>
	public class ArgProcessUserException : Exception {
		public ArgProcessUserException(string message) : base(message) {}
	}


	/// <summary>
	/// Throw this when doing console parsing if a programmer made a mistake.
	/// </summary>
	public class ArgProcessProgrammerException: Exception {
		public ArgProcessProgrammerException(string message) : base(message) {}
	}
}
