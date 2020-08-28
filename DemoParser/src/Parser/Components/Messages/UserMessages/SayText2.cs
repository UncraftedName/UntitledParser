using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages.UserMessages {
	
	public class SayText2 : UserMessage {

		public byte Client;
		public bool WantsToChat;
		
		
		public SayText2(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			Client = bsr.ReadByte();
			WantsToChat = bsr.ReadBool();
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void AppendToWriter(IndentedWriter iw) {
			iw.AppendLine($"client: {Client}");
			iw.Append($"wants to chat: {WantsToChat}");
		}
	}
}