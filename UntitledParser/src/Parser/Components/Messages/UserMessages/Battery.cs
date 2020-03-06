using UntitledParser.Parser.Components.Abstract;
using UntitledParser.Utils;

namespace UntitledParser.Parser.Components.Messages.UserMessages {
	
	public class Battery : SvcUserMessage {

		// range: 0 - 100
		public ushort BatteryVal;

		public Battery(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			BatteryVal = bsr.ReadUShort();
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new System.NotImplementedException();
		}


		internal override void AppendToWriter(IndentedWriter iw) {
			iw += $"Battery: {BatteryVal}";
		}
	}
}