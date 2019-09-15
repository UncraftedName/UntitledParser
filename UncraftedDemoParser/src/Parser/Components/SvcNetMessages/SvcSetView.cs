using UncraftedDemoParser.Parser.Components.Abstract;
using UncraftedDemoParser.Utils;

namespace UncraftedDemoParser.Parser.Components.SvcNetMessages {
	
	public class SvcSetView : SvcNetMessage {

		
		public int EntityIndex;
		
		
		public SvcSetView(byte[] data, SourceDemo demoRef, int tick) : base(data, demoRef, tick) {}


		protected override void ParseBytes(BitFieldReader bfr) {
			EntityIndex = bfr.ReadBitsAsInt(11);
		}
	}
}