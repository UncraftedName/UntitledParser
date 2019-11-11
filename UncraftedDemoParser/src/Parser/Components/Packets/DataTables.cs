using UncraftedDemoParser.Parser.Components.Abstract;

namespace UncraftedDemoParser.Parser.Components.Packets {
	
	public class DataTables : DemoPacket{
		
		public DataTables(byte[] data, SourceDemo demoRef, int tick) : base(data, demoRef, tick) {}
	}
}