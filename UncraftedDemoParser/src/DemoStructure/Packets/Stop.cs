using UncraftedDemoParser.DemoStructure.Packets.Abstract;

namespace UncraftedDemoParser.DemoStructure.Packets {
	
	public class Stop : DemoPacket {
		
		public Stop(SourceDemo demoRef, int tick) : base(new byte[0], demoRef, tick) {}


		public override void ParseBytes() {}

		
		public override void UpdateBytes() {}


		public override string ToString() {
			return "";
		}
	}
}