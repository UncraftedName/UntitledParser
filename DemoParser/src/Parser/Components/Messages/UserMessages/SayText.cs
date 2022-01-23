using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages.UserMessages {

	public class SayText : UserMessage {

		public byte ClientId;
		public string Str;
		public bool WantsToChat;
		//public bool? Unknown; // this is either "chat" or "text all chat"


		public SayText(SourceDemo? demoRef) : base(demoRef) {}


		protected override void Parse(ref BitStreamReader bsr) {
			ClientId = bsr.ReadByte();
			Str = bsr.ReadNullTerminatedString();
			WantsToChat = bsr.ReadByte() != 0;
			/*if (DemoInfo.NewDemoProtocol)
				Unknown = bsr.ReadByte() != 0;*/
		}


		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.AppendLine($"client ID: {ClientId}");
			pw.AppendLine($"string: {Str.Replace("\n", @"\n")}");
			pw.Append($"wants to chat: {WantsToChat}");
			/*if (DemoInfo.NewDemoProtocol)
				iw.Append($"unknown: {Unknown}");*/
		}
	}
}
