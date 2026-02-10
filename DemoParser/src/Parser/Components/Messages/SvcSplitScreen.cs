using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {

	// format is grabbed from p2 dissasembly + strike protobuf messages
	public class SvcSplitScreen : DemoMessage {

		public bool RemoveUser;
		public int Slot;
		public int PlayerIndex;


		public SvcSplitScreen(SourceDemo? demoRef, byte value) : base(demoRef, value) {}


		protected override void Parse(ref BitStreamReader bsr) {
			RemoveUser = bsr.ReadBool();
			Slot = bsr.ReadBool() ? 1 : 0;
			PlayerIndex = bsr.ReadSInt(DemoInfo.MaxEdictBits);
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.AppendLine(RemoveUser ? "remove user" : "add user");
			pw.AppendLine($"slot: {Slot}");
			pw.Append($"player index: {PlayerIndex}");
		}
	}
}
