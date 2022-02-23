using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages.UserMessages.Haptic {

	public class HapPunch : UserMessage {

		public float F1, F2, F3;


		public HapPunch(SourceDemo? demoRef, byte value) : base(demoRef, value) {}


		protected override void Parse(ref BitStreamReader bsr) {
			F1 = bsr.ReadFloat();
			F2 = bsr.ReadFloat();
			F3 = bsr.ReadFloat();
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.Append($"unknown floats: ({F1}, {F2}, {F3})");
		}
	}
}
