using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages.UserMessages {

	public class ScoreboardTempUpdate : UserMessage {

		public int NumPortals;
		public int TimeTaken; // in centi-seconds

		public ScoreboardTempUpdate(SourceDemo? demoRef, byte value) : base(demoRef, value) {}

		protected override void Parse(ref BitStreamReader bsr) {
			NumPortals = bsr.ReadSInt();
			TimeTaken = bsr.ReadSInt();
		}

		public override void PrettyWrite(IPrettyWriter pw) {
			pw.AppendLine($"num portals: {NumPortals}");
			pw.Append($"time taken: {TimeTaken / 100.0f}");
		}
	}
}
