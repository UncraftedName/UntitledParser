using UncraftedDemoParser.Parser.Components.Abstract;
using UncraftedDemoParser.Utils;

namespace UncraftedDemoParser.Parser.Components.SvcNetMessages {
	
	public class SvcPrefetch : SvcNetMessage {


		public int SoundIndex;
		
		
		public SvcPrefetch(byte[] data, SourceDemo demoRef, int tick) : base(data, demoRef, tick) {}
		
		
		protected override void ParseBytes(BitFieldReader bfr) {
			SoundIndex = bfr.ReadBitsAsInt(13);
		}
	}
}