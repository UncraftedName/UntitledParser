using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {

	public class NetStringCmd : DemoMessage {

		public string Command;

		public NetStringCmd(SourceDemo? demoRef, byte value) : base(demoRef, value) {}


		protected override void Parse(ref BitStreamReader bsr) {
			Command = bsr.ReadNullTerminatedString();
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			// replace new lines with literal '\n' because every time i've seen it come up the new line
			// has always been the last character, and it's just annoying to have a blank line
			pw.Append($"Command: {Command.ToLiteral()}");
		}
	}
}
