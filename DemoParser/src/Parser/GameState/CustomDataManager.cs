using System.Collections.Generic;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.Components.Packets.CustomDataTypes;

namespace DemoParser.Parser.GameState {

	public class CustomDataManager {

		private readonly SourceDemo _demoRef;
		private readonly List<string> _callbacks;


		public CustomDataManager(SourceDemo demo, List<string> callbackTypes) {
			_callbacks = callbackTypes;
			_demoRef = demo;
		}


		public string? GetDataType(int val) {
			if (val < 0 || val >= _callbacks.Count) {
				_demoRef.LogError($"{GetType().Name}: unregistered custom data with value {val}");
				return null;
			}
			return _callbacks[val];
		}


		public CustomDataMessage CreateCustomDataMessage(string? type) {
			return type switch {
				"RadialMenuMouseCallback" => new RadialMouseMenuCallback (_demoRef),
				_                         => new UnknownCustomDataMessage(_demoRef)
			};
		}
	}


	public abstract class CustomDataMessage : DemoComponent {
		protected CustomDataMessage(SourceDemo? demoRef, int? decompressedIndex = null) : base(demoRef, decompressedIndex) {}
	}
}
