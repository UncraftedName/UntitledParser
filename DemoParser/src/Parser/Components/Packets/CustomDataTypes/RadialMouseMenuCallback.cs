using DemoParser.Parser.GameState;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Packets.CustomDataTypes {

	public class RadialMouseMenuCallback : CustomDataMessage {

		public uint X, Y;

		public RadialMouseMenuCallback(SourceDemo? demoRef) : base(demoRef) {}


		protected override void Parse(ref BitStreamReader bsr) {
			X = bsr.ReadUInt();
			Y = bsr.ReadUInt();
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.Append($"ping screen location (x, y): ({X}, {Y})");
		}
	}
}
