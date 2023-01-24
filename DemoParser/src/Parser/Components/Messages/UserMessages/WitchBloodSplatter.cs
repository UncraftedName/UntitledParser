using System.Numerics;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages.UserMessages {

	public class WitchBloodSplatter : UserMessage {

		public Vector3 Pos;


		public WitchBloodSplatter(SourceDemo? demoRef, byte value) : base(demoRef, value) {}

		protected override void Parse(ref BitStreamReader bsr) {
			bsr.ReadVectorCoord(out Pos);
		}

		public override void PrettyWrite(IPrettyWriter pw) {
			pw.Append($"pos: {Pos}");
		}
	}
}
