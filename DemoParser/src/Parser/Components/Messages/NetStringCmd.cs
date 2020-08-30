using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {
	
	public class NetStringCmd : DemoMessage {
	
		public string Command;

		public NetStringCmd(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}


		internal override void ParseStream(BitStreamReader bsr) {
			Command = bsr.ReadNullTerminatedString();
			SetLocalStreamEnd(bsr);
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void AppendToWriter(IndentedWriter iw) {
			// replace new lines with literal '\n' because every time i've seen it come up the new line
			// has always been the last character, and it's just annoying to have a blank line
			iw.Append($"Command: {Command.Replace("\n", @"\n")}");
		}
	}
}