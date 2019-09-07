using UncraftedDemoParser.DemoStructure.Components.Abstract;
using UncraftedDemoParser.Utils;

namespace UncraftedDemoParser.DemoStructure.Components {
	
	public class SvcTempEntities : SvcNetMessage {


		public byte NumEntries;
		public int DataBitLength;
		public byte[] Data;
		
		
		public SvcTempEntities(byte[] data, SourceDemo demoRef, int tick): base(data, demoRef, tick) {}
		
		
		protected override void ParseBytes(BitFieldReader bfr) {
			NumEntries = bfr.ReadByte();
			DataBitLength = bfr.ReadBitsAsInt(17);
			Data = bfr.ReadBits(DataBitLength);
		}
	}
}