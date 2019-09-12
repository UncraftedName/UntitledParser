using UncraftedDemoParser.Parser.Components.Abstract;
using UncraftedDemoParser.Utils;

namespace UncraftedDemoParser.Parser.Components.NetSvcMessages {
	
	public class NetNop : SvcNetMessage {
		
		
		public NetNop(byte[] data, SourceDemo demoRef, int tick) : base(data, demoRef, tick) {}
		
		
		// byte[] {0, 0, 0, 0, 0}
		protected override void ParseBytes(BitFieldReader bfr) {}
	}
}