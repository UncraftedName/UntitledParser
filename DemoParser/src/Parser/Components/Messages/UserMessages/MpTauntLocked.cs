using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages.UserMessages {
	
	public class MpTauntLocked : SvcUserMessage {
		
		public string TauntName;
		
		
		public MpTauntLocked(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			TauntName = bsr.ReadNullTerminatedString();
		}
		
		
		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new System.NotImplementedException();
		}
		
		
		public override void AppendToWriter(IndentedWriter iw) {
			iw.Append($"taunt name: {TauntName}");
		}
	}
}
