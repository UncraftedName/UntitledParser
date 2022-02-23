using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Packets {

	/// <summary>
	/// Signifies the last packet. Contains no data.
	/// </summary>
	public class Stop : DemoPacket {

		public override bool MayContainData => false;


		public Stop(SourceDemo? demoRef, PacketFrame frameRef) : base(demoRef, frameRef) {}


		protected override void Parse(ref BitStreamReader bsr) {}
	}
}
