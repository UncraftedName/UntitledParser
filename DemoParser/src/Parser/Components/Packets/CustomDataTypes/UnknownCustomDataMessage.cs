using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;

namespace DemoParser.Parser.Components.Packets.CustomDataTypes {
	
	public class UnknownCustomDataMessage : CustomDataMessage {

		public UnknownCustomDataMessage(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new System.NotImplementedException();
		}


		internal override void AppendToWriter(IndentedWriter iw) {
			iw += $"unknown data: {Reader.ToHexString()}";
		}
	}
}