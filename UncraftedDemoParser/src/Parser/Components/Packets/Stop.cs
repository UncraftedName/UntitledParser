using UncraftedDemoParser.Parser.Components.Abstract;

namespace UncraftedDemoParser.Parser.Components.Packets {
	
	public class Stop : DemoPacket {
		
		public Stop(SourceDemo demoRef, int tick) : base(new byte[0], demoRef, tick) {}


		public override string ToString() {
			return "";
		}
	}
}