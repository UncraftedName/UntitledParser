using System;
using UncraftedDemoParser.Parser.Components.Abstract;

namespace UncraftedDemoParser.Parser.Misc {
	
	public class FailedToParseException : Exception {
		
		
		public int? Tick {get;}
		public DemoComponent Component {get;}
		

		public FailedToParseException(DemoComponent component, int? tick = null) {
			Tick = tick;
			Component = component;
		}


		public override string ToString() {
			return $"Failed to parse {Component.GetType()}{(Tick.HasValue ? $" on tick {Tick}" : "")}";
		}
	}
}