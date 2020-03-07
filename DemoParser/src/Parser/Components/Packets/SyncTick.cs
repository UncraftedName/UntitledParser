using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;

namespace DemoParser.Parser.Components.Packets {
	
	public class SyncTick : DemoPacket {

		public override bool MayContainData => false;
		

		public SyncTick(SourceDemo demoRef, BitStreamReader reader, int tick) : base(demoRef, reader, tick) {}


		internal override void ParseStream(BitStreamReader bsr) {
			SetLocalStreamEnd(bsr);
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {}
	}
}