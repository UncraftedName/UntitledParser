using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages.UserMessages {

	public class SayText2 : UserMessage {

		public byte Client;
		public bool WantsToChat;


		public SayText2(SourceDemo? demoRef, byte value) : base(demoRef, value) {}


		protected override void Parse(ref BitStreamReader bsr) {
			Client = bsr.ReadByte();
			WantsToChat = bsr.ReadBool();
			// TODO: fill out the rest of the UserMessage (don't know what SayText2
			// is even but I guess it doesn't happen often at all since it hasn't broken a demo parse yet)
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.AppendLine($"client: {Client}");
			pw.Append($"wants to chat: {WantsToChat}");
		}
	}
}
