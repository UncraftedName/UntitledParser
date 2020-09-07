using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages.UserMessages {
	
	public class MpTauntEarned : UserMessage {

		public string TauntName;
		public bool AwardSilently;
		
		
		public MpTauntEarned(SourceDemo? demoRef) : base(demoRef) {}


		protected override void Parse(ref BitStreamReader bsr) {
			TauntName = bsr.ReadNullTerminatedString();
			AwardSilently = bsr.ReadByte() != 0;
		}
		
		
		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}
		
		
		public override void AppendToWriter(IIndentedWriter iw) {
			iw.AppendLine($"taunt name: {TauntName}");
			iw.Append($"award silently: {AwardSilently}");
		}
	}
}
