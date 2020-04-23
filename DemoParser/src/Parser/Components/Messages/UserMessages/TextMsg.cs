using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages.UserMessages {
	
	public class TextMsg : SvcUserMessage {

		public const int MessageCount = 5;
		public TextMsgDestination Destination;
		public string[] Messages;
		
		
		public TextMsg(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			Destination = (TextMsgDestination)bsr.ReadByte();
			Messages = new string[MessageCount];
			for (int i = 0; i < MessageCount; i++) 
				Messages[i] = bsr.ReadNullTerminatedString().Replace("\n", @"\n");
		}


		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new System.NotImplementedException();
		}


		public override void AppendToWriter(IndentedWriter iw) {
			iw.Append("destination: " + Destination);
			for (int i = 0; i < MessageCount; i++) 
				iw.Append($"\nmessage {i + 1}: {Messages[i]}");
		}
	}


	public enum TextMsgDestination : byte {
		PrintNotify = 1,
		PrintConsole,
		PrintTalk,
		PrintCenter
	}
}