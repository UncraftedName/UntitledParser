using System;

namespace ConsoleApp.ConsoleParsing {
	
	public static class Utils {}


	public class ArgProcessException : Exception {
		public ArgProcessException(string message) : base(message) {}
	}
}