using System;
using UncraftedDemoParser.Parser.Components.Abstract;

namespace UncraftedDemoParser.Parser.Misc {
	
	public class FailedToParseException : Exception {
		
		
		public int? Tick {get;}
		public DemoComponent Component {get;}


		public FailedToParseException(DemoComponent component, int? tick = null) : base($"Failed to parse {component.GetType().Name}{(tick.HasValue ? $" on tick {tick}" : "")}"){
			Tick = tick;
			Component = component;
		}

		public FailedToParseException(string message) : base(message) {}


		public override string ToString() {
			if (Component == null)
				return base.ToString();
			return $"Failed to parse {Component.GetType().Name}{(Tick.HasValue ? $" on tick {Tick}" : "")}";
		}
	}
}