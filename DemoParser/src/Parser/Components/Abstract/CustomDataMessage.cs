using DemoParser.Parser.Components.Packets.CustomDataTypes;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Abstract {
	
	// used in the custom data packet which can contain different kinds of data
	public abstract class CustomDataMessage : DemoComponent {
		protected CustomDataMessage(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
	}


	public static class CustomDataFactory {

		// pass in a substream of correct length
		public static CustomDataMessage CreateCustomDataMessage(SourceDemo demoRef, BitStreamReader bsr, CustomDataType type) {
			CustomDataMessage result = type switch {
				CustomDataType.MenuCallback => new MenuCallBack(demoRef, bsr),
				CustomDataType.Ping 		=> new Ping(demoRef, bsr),
				_ => new UnknownCustomDataMessage(demoRef, bsr)
			};
			
			if (result.GetType() == typeof(UnknownCustomDataMessage))
				demoRef.AddError($"unknown or unimplemented custom data type: {type}");
			return result;
		}
	}


	public enum CustomDataType {
		MenuCallback 	= -1,
		Ping			= 0
	}
}