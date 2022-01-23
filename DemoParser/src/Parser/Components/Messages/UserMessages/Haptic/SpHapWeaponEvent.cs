using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages.UserMessages.Haptic {

	public class SpHapWeaponEvent : UserMessage {

		public int Unk;


		public SpHapWeaponEvent(SourceDemo? demoRef) : base(demoRef) {}


		protected override void Parse(ref BitStreamReader bsr) {
			Unk = bsr.ReadSInt();
		}


		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new System.NotImplementedException();
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.Append($"unknown int: {Unk}");
		}
	}
}
