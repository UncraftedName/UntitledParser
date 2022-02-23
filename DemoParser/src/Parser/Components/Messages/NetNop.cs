using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {

	// nop command used for padding
	public class NetNop : DemoMessage {

		public override bool MayContainData => false;


		public NetNop(SourceDemo? demoRef, byte value) : base(demoRef, value) {}


		protected override void Parse(ref BitStreamReader bsr) {}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.Append("NetNop");
		}
	}
}
