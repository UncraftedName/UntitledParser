using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Packets {
	
	/// <summary>
	/// Synchronizes server and client clock. Contains no data.
	/// </summary>
	public class SyncTick : DemoPacket {

		public override bool MayContainData => false;
		

		public SyncTick(SourceDemo? demoRef, int tick) : base(demoRef, tick) {}


		protected override void Parse(ref BitStreamReader bsr) {
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {}
	}
}