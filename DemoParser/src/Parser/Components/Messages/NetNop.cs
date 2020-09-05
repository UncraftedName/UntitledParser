using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {
	
	// nop command used for padding
	public class NetNop : DemoMessage {
		
		public override bool MayContainData => false;
		

		public NetNop(SourceDemo? demoRef) : base(demoRef) {}


		protected override void Parse(ref BitStreamReader bsr) {
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {}
	}
}