using UntitledParser.Parser.Components.Abstract;
using UntitledParser.Utils;

namespace UntitledParser.Parser.Components.Messages.UserMessages {
	
	public class Train : SvcUserMessage {

		public byte Pos;
		
		
		public Train(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			Pos = bsr.ReadByte();
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new System.NotImplementedException();
		}


		internal override void AppendToWriter(IndentedWriter iw) {
			iw += $"pos: {Pos}";
		}
	}
}