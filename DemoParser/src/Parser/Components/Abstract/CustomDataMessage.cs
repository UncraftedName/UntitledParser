using DemoParser.Parser.Components.Packets.CustomDataTypes;

namespace DemoParser.Parser.Components.Abstract {
	
	/// <summary>
	/// A 'sub-packet' in the CustomData packet.
	/// </summary>
	public abstract class CustomDataMessage : DemoComponent {
		protected CustomDataMessage(SourceDemo? demoRef) : base(demoRef) {}
	}


	public static class CustomDataFactory {

		// pass in a substream of correct length
		public static CustomDataMessage CreateCustomDataMessage(SourceDemo? demoRef, CustomDataType type) {
			CustomDataMessage result = type switch {
				CustomDataType.MenuCallback => new MenuCallBack(demoRef),
				CustomDataType.Ping         => new Ping(demoRef),
				_ => new UnknownCustomDataMessage(demoRef) 
			};
			
			if (result.GetType() == typeof(UnknownCustomDataMessage))
				demoRef.LogError($"unknown or unimplemented custom data type: {type}");
			return result;
		}
	}


	public enum CustomDataType {
		MenuCallback = -1,
		Ping         = 0
	}
}