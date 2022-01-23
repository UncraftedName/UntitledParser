using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages.UserMessages {

	public class TextMsg : UserMessage {

		public const int MessageCount = 5;
		public TextMsgDestination Destination;
		public string[] Messages;


		public TextMsg(SourceDemo? demoRef) : base(demoRef) {}


		protected override void Parse(ref BitStreamReader bsr) {
			Destination = (TextMsgDestination)bsr.ReadByte();
			Messages = new string[MessageCount];
			for (int i = 0; i < MessageCount; i++)
				Messages[i] = bsr.ReadNullTerminatedString();
		}


		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.Append($"destination: {Destination}");
			for (int i = 0; i < MessageCount; i++)
				pw.Append($"\nmessage {i + 1}: {Messages[i].Replace("\n", @"\n")}");
		}
	}


	public enum TextMsgDestination : byte {
		PrintNotify = 1,
		PrintConsole,
		PrintTalk,
		PrintCenter
	}
}
