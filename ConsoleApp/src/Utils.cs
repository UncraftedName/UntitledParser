using System;
using System.Collections.Generic;

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
			for (int i = 0; i < s1.Length && i < s2.Length && s1[i] == s2[i]; i++) {
				if (s1[i] == '/' || s1[i] == '\\')
					furthestSlash = i;
			}
			return s1.Substring(0, furthestSlash);
		}
	}


	public class ArgProcessException : Exception {
		public ArgProcessException(string message) : base(message) {}
	}
}