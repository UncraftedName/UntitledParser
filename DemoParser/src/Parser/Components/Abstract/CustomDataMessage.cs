using DemoParser.Parser.Components.Packets.CustomDataTypes;

namespace DemoParser.Parser.Components.Abstract {

	/// <summary>
	/// A 'sub-packet' in the CustomData packet.
	/// </summary>
	public abstract class CustomDataMessage : DemoComponent {
		protected CustomDataMessage(SourceDemo? demoRef) : base(demoRef) {}
	}


	public static class CustomDataFactory {

		public static CustomDataMessage CreateCustomDataMessage(SourceDemo? dRef, CustomDataType type) {
			return type switch {
				CustomDataType.MenuCallback => new MenuCallBack(dRef),
				CustomDataType.Ping         => new Ping(dRef),
				_ => new UnknownCustomDataMessage(dRef)
			};
		}
	}


	public enum CustomDataType {
		MenuCallback = -1,
		Ping         =  0
	}
}
