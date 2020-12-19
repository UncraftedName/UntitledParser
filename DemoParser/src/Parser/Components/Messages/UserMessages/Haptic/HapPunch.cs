using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages.UserMessages.Haptic {
	
	public class HapPunch : UserMessage {

		public float F1, F2, F3;
		
		
		public HapPunch(SourceDemo? demoRef) : base(demoRef) {}
		
		
		protected override void Parse(ref BitStreamReader bsr) {
			F1 = bsr.ReadFloat();
			F2 = bsr.ReadFloat();
			F3 = bsr.ReadFloat();
		}


		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new System.NotImplementedException();
		}


		public override void AppendToWriter(IIndentedWriter iw) {
			iw.Append($"unknown floats: ({F1}, {F2}, {F3})");
		}
	}
}