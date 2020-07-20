using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages.UserMessages {
	
	public class HudText : SvcUserMessage {

		public string Str;
		
		
		public HudText(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			Str = bsr.ReadNullTerminatedString();
		}
		
		
		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new System.NotImplementedException();
		}


		public override void AppendToWriter(IndentedWriter iw) {
			iw.Append(Str);
		}
	}
}