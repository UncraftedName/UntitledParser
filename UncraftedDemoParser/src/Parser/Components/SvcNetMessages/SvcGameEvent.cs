using UncraftedDemoParser.Parser.Components.Abstract;
using UncraftedDemoParser.Utils;

namespace UncraftedDemoParser.Parser.Components.SvcNetMessages {
	
	public class SvcGameEvent : SvcNetMessage {


		public int DataBitCount; // would be nice to not store this
		public byte[] Data;
		
		
		public SvcGameEvent(byte[] data, SourceDemo demoRef, int tick) : base(data, demoRef, tick) {}
		
		
		protected override void ParseBytes(BitFieldReader bfr) {
			DataBitCount = bfr.ReadBitsAsInt(11);
			Data = bfr.ReadBits(DataBitCount);
			// todo - game event manager
		}
	}
}