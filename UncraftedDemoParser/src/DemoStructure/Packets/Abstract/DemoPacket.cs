namespace UncraftedDemoParser.DemoStructure.Packets.Abstract {
	
	// doesn't have to be a packet. used for everything that isn't the header or packet frame
	public abstract class DemoPacket : DemoComponent {

		// in the demo this is stored as part of the packet frame,
		// but in the this parser it's stored in the packets for easy access
		public int Tick;
		
		protected DemoPacket(byte[] data, SourceDemo demoRef, int tick) : base(data, demoRef) {
			Tick = tick;
		}
	}
}