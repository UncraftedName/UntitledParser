using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {

	public class SvcSetPause : DemoMessage {

		public bool IsPaused;

		public SvcSetPause(SourceDemo? demoRef, byte value) : base(demoRef, value) {}


		protected override void Parse(ref BitStreamReader bsr) {
			IsPaused = bsr.ReadBool();
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.Append($"is paused: {IsPaused}");
		}
	}
}
