using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ConsoleApp {

	public static class Utils {

		private static readonly Stack<ConsoleColor> ForegroundColors = new Stack<ConsoleColor>();
		
		
		public static void PushForegroundColor(ConsoleColor color) {
			ForegroundColors.Push(color);
			Console.ForegroundColor = color;
		}


		public static ConsoleColor PopForegroundColor() {
			return ForegroundColors.Pop();
		}


		public static void WriteColor(string s, ConsoleColor color) {
			PushForegroundColor(color);
			Console.Write(s);
			PopForegroundColor();
		}
		
		
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


		// TODO - add special cases here, e.g. "None" for flags
		public static T ParseEnum<T>(string arg) where T : Enum {
			return (T)Enum.Parse(typeof(T), arg, true);
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