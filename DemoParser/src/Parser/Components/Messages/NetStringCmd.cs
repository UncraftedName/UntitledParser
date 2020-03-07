using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;

namespace DemoParser.Parser.Components.Messages {
	
	public class NetStringCmd : DemoMessage {
	
		public string Command;

		public NetStringCmd(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}


		internal override void ParseStream(BitStreamReader bsr) {
			Command = bsr.ReadNullTerminatedString();
			SetLocalStreamEnd(bsr);
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new System.NotImplementedException();
		}


		internal override void AppendToWriter(IndentedWriter iw) {
			iw += $"Command: {Command.Replace("\n", @"\n")}";
		}
	}
}