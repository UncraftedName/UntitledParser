using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages.UserMessages.Haptic {

	public class SpHapWeaponEvent : UserMessage {

		public int Unk;


		public SpHapWeaponEvent(SourceDemo? demoRef, byte value) : base(demoRef, value) {}


		protected override void Parse(ref BitStreamReader bsr) {
			Unk = bsr.ReadSInt();
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.Append($"unknown int: {Unk}");
		}
	}
}
