using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages.UserMessages.Haptic {

	public class HapSetConstForce : UserMessage {

		public short S1, S2, S3;


		public HapSetConstForce(SourceDemo? demoRef, byte value) : base(demoRef, value) {}


		protected override void Parse(ref BitStreamReader bsr) {
			S1 = bsr.ReadSShort();
			S2 = bsr.ReadSShort();
			S3 = bsr.ReadSShort();
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.Append($"unknown shorts: ({S1}, {S2}, {S3})");
		}
	}
}
