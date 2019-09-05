namespace UncraftedDemoParser.DemoStructure.Packets {
	// used to describe the various components of a demo,
	// constructed with a byte array, and contains fields that can be updated and rewritten to a new byte array
	public abstract class DemoComponent {

		public byte[] Bytes {get;protected set;}
		protected SourceDemo _demoRef;
		

		protected DemoComponent(byte[] data, SourceDemo demoRef) {
			Bytes = data;
			_demoRef = demoRef;
		}


		// call this to populate the fields from the byte array
		public abstract void ParseBytes();


		// If any fields of a packet are ever updated, call this to get the modified byte array.
		// Behavior is undefined if fields are updated and this isn't called.
		public abstract void UpdateBytes();


		public override string ToString() {
			return "Not implemented";
		}
	}
}