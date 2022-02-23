using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Packets.CustomDataTypes {

	public class UnknownCustomDataMessage : CustomDataMessage {

		public UnknownCustomDataMessage(SourceDemo? demoRef) : base(demoRef) {}


		protected override void Parse(ref BitStreamReader bsr) {}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.Append($"unknown data: {Reader.ToHexString()}");
		}
	}
}
