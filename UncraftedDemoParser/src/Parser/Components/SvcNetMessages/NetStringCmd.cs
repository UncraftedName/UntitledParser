using System.Text;
using UncraftedDemoParser.Parser.Components.Abstract;
using UncraftedDemoParser.Utils;

namespace UncraftedDemoParser.Parser.Components.SvcNetMessages {
	
	public class NetStringCmd : SvcNetMessage {

		public string Command;
		
		
		public NetStringCmd(byte[] data, SourceDemo demoRef, int tick) : base(data, demoRef, tick) {}
		
		
		protected override void ParseBytes(BitFieldReader bfr) {
			Command = bfr.ReadNullTerminatedString();
		}


		protected override void PopulatedBuilder(StringBuilder builder) {
			builder.Append($"\t\t{Command}");
		}
	}
}