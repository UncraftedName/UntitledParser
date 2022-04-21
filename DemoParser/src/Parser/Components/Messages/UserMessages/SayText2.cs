using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages.UserMessages {

	public class SayText2 : UserMessage {

		public byte Client;
		public bool WantsToChat;
		public string MsgName;
		public string[] Msgs;


		public SayText2(SourceDemo? demoRef, byte value) : base(demoRef, value) {}


		protected override void Parse(ref BitStreamReader bsr) {
			Client = bsr.ReadByte();
			WantsToChat = bsr.ReadByte() != 0;
			MsgName = bsr.ReadNullTerminatedString();
			Msgs = new string[4];
			for (int i = 0; i < Msgs.Length; i++)
				Msgs[i] = bsr.ReadNullTerminatedString();
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.AppendLine($"client: {Client}");
			pw.AppendLine($"wants to chat: {WantsToChat}");
			pw.Append($"name: {MsgName}");
			pw.FutureIndent++;
			for (var i = 0; i < Msgs.Length; i++)
				pw.Append($"\nmessage {i + 1}: {Msgs[i]}");
			pw.FutureIndent--;
		}
	}
}
