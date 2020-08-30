using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages.UserMessages {
	
	// since I know how to skip unknown/unimplemented message types and I don't want a random BitStreamReader in the UserMessageFrame class,
	// I will just pass it to this class if I don't know how to parse it yet
	public sealed class UnknownUserMessage : UserMessage {
		
		// make sure to pass in a substream here
		public UnknownUserMessage(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}


		internal override void ParseStream(BitStreamReader bsr) {}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void AppendToWriter(IndentedWriter iw) {
			iw.Append($"{Reader.BitLength / 8} bytes: {Reader.ToHexString()}");
		}
	}
}