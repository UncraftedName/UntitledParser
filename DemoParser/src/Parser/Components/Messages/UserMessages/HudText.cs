using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages.UserMessages {

	public class HudText : UserMessage {

		public string Str;


		public HudText(SourceDemo? demoRef, byte value) : base(demoRef, value) {}


		protected override void Parse(ref BitStreamReader bsr) {
			Str = bsr.ReadStringOfLength(bsr.BitsRemaining / 8);
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.Append(Str);
		}
	}
}
