using UncraftedDemoParser.Parser.Components.Abstract;
using UncraftedDemoParser.Utils;

namespace UncraftedDemoParser.Parser.Components.SvcNetMessages {
	
	public class SvcUserMessage : SvcNetMessage {


		private int BitsForDataLength => DemoRef.DemoSettings.NewEngine ? 12 : 11;
		
		public byte MessageType;
		public int DataLength; // would be nice to not have this accessible to user
		public byte[] MsgData;


		public SvcUserMessage(byte[] data, SourceDemo demoRef, int tick) : base(data, demoRef, tick) {}
		
		
		protected override void ParseBytes(BitFieldReader bfr) {
			MessageType = bfr.ReadByte();
			DataLength = bfr.ReadBitsAsInt(BitsForDataLength);
			MsgData = bfr.ReadBits(DataLength);
		}
	}
}