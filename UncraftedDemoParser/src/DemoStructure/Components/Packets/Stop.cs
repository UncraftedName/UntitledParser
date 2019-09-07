using UncraftedDemoParser.DemoStructure.Components.Abstract;

namespace UncraftedDemoParser.DemoStructure.Components.Packets {
	
	public class Stop : DemoPacket {
		
		public Stop(SourceDemo demoRef, int tick) : base(new byte[0], demoRef, tick) {}


		public override void ParseBytes() {}

		
		public override void UpdateBytes() {}


		public override string ToString() {
			return "";
		}
	}
}