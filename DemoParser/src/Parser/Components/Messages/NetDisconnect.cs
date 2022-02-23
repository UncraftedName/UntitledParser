using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {

	// last message in connection
	public class NetDisconnect : DemoMessage {

		public string Reason;


		public NetDisconnect(SourceDemo? demoRef, byte value) : base(demoRef, value) {}


		protected override void Parse(ref BitStreamReader bsr) {
			Reason = bsr.ReadNullTerminatedString();
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.Append($"reason: {Reason}");
		}
	}
}
