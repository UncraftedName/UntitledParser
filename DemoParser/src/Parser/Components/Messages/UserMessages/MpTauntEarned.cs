using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages.UserMessages {
	
	public class MpTauntEarned : SvcUserMessage {

		public string TauntName;
		public bool AwardSilently;
		
		
		public MpTauntEarned(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			TauntName = bsr.ReadNullTerminatedString();
			AwardSilently = bsr.ReadByte() != 0;
		}
		
		
		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new System.NotImplementedException();
		}
		
		
		public override void AppendToWriter(IndentedWriter iw) {
			iw.AppendLine($"taunt name: {TauntName}");
			iw.Append($"award silently: {AwardSilently}");
		}
	}
}
