using UntitledParser.Parser.Components.Abstract;
using UntitledParser.Utils;

namespace UntitledParser.Parser.Components.Messages.UserMessages {
	
	// since I know how to skip unknown/unimplemented message types and I don't want a random BitStreamReader in the UserMessageFrame class,
	// I will just pass it to this class if I don't know how to parse it yet
	public sealed class UnknownSvcUserMessage : SvcUserMessage {
		
		// make sure to pass in a substream here
		public UnknownSvcUserMessage(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}


		internal override void ParseStream(BitStreamReader bsr) {}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new System.NotImplementedException();
		}


		internal override void AppendToWriter(IndentedWriter iw) {
			iw +=  $"{Reader.BitLength / 8} bytes: {Reader.ToHexString()}";
		}
	}
}