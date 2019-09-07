using UncraftedDemoParser.DemoStructure.Components.Abstract;
using UncraftedDemoParser.Utils;

namespace UncraftedDemoParser.DemoStructure.Components.NetSvcMessages {
	
	public class SvcSetView : SvcNetMessage {

		
		public int EntityIndex;
		
		
		public SvcSetView(byte[] data, SourceDemo demoRef, int tick) : base(data, demoRef, tick) {}


		protected override void ParseBytes(BitFieldReader bfr) {
			EntityIndex = bfr.ReadBitsAsInt(11);
		}
	}
}