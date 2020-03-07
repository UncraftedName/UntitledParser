using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;

namespace DemoParser.Parser.Components.Packets {
	
	public class Stop : DemoPacket {

		public override bool MayContainData => false;
		

		public Stop(SourceDemo demoRef, BitStreamReader reader, int tick) : base(demoRef, reader, tick) {}


		internal override void ParseStream(BitStreamReader bsr) {}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {}
	}
}