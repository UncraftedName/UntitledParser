using UntitledParser.Parser.Components.Abstract;
using UntitledParser.Utils;

namespace UntitledParser.Parser.Components.Messages.UserMessages {
	
	public class SayText2 : SvcUserMessage {

		public byte Client;
		public bool WantsToChat;
		
		
		public SayText2(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			Client = bsr.ReadByte();
			WantsToChat = bsr.ReadBool();
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new System.NotImplementedException();
		}


		internal override void AppendToWriter(IndentedWriter iw) {
			iw.AppendLine($"client: {Client}");
			iw.Append($"wants to chat: {WantsToChat}");
		}
	}
}