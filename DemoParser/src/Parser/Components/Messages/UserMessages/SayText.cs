using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages.UserMessages {
	
	public class SayText: SvcUserMessage {
		
		public byte ClientId;
		public string Str;
		public bool? Unknown; // todo this is either "chat" or "text all chat"


		public SayText(SourceDemo demoRef, BitStreamReader reader): base(demoRef, reader) {}


		internal override void ParseStream(BitStreamReader bsr) {
			ClientId = bsr.ReadByte();
			Str = bsr.ReadNullTerminatedString();
			if (DemoSettings.NewDemoProtocol)
				Unknown = bsr.ReadByte() != 0;
		}


		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new System.NotImplementedException();
		}


		public override void AppendToWriter(IndentedWriter iw) {
			iw.AppendLine($"client ID: {ClientId}");
			iw.AppendLine($"string: {Str.Replace("\n", @"\n")}");
			if (DemoSettings.NewDemoProtocol)
				iw.Append($"unknown: {Unknown}");
		}
	}
}
