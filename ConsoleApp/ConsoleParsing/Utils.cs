using System;
using System.Collections.Generic;

namespace ConsoleApp.ConsoleParsing {

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
	}


	public class ArgProcessException : Exception {
		public ArgProcessException(string message) : base(message) {}
	}
}