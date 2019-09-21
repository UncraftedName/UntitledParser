using System.Text;
using UncraftedDemoParser.Parser.Components.Abstract;
using UncraftedDemoParser.Utils;

namespace UncraftedDemoParser.Parser.Components.SvcNetMessages {
	
	public class NetNop : SvcNetMessage {
		
		
		public NetNop(byte[] data, SourceDemo demoRef, int tick) : base(data, demoRef, tick) {}
		
		
		// byte[] {0, 0, 0, 0, 0}
		protected override void ParseBytes(BitFieldReader bfr) {}


		protected override void PopulatedBuilder(StringBuilder builder) {
			builder.AppendLine("\t\tno data");
		}
	}
}