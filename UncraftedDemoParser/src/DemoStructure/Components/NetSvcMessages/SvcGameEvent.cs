using UncraftedDemoParser.DemoStructure.Components.Abstract;
using UncraftedDemoParser.Utils;

namespace UncraftedDemoParser.DemoStructure.Components.NetSvcMessages {
	
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