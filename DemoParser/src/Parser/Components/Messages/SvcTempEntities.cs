using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {

	public class SvcTempEntities : DemoMessage {

		public byte EntryCount;
		private BitStreamReader _data;
		public BitStreamReader Data => _data.FromBeginning();

		public SvcTempEntities(SourceDemo? demoRef, byte value) : base(demoRef, value) {}


		protected override void Parse(ref BitStreamReader bsr) {
			EntryCount = bsr.ReadByte();
			uint bitLen = DemoInfo.Game is SourceGame.PORTAL_1_1910503 or SourceGame.TF2
				? bsr.ReadVarUInt32()
				: bsr.ReadUInt(DemoInfo.Game.IsLeft4Dead2() ? 18 : 17);
			_data = bsr.ForkAndSkip((int)bitLen);

			// todo
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.AppendLine($"entry count: {EntryCount}");
			pw.Append($"length in bits: {_data.BitLength}");
		}
	}
}
