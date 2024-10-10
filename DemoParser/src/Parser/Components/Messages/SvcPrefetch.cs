using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.GameState;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {

	public class SvcPrefetch : DemoMessage {

		public int SoundIndex;
		public string? SoundName;


		public SvcPrefetch(SourceDemo? demoRef, byte value) : base(demoRef, value) {}


		protected override void Parse(ref BitStreamReader bsr) {
			var soundIndexBits = DemoInfo.Game.IsLeft4Dead2() ? DemoInfo.Game >= SourceGame.L4D2_2091 ? 15 : 14 : 13;
			SoundIndex = (int)bsr.ReadUInt(soundIndexBits);

			var mgr = GameState.StringTablesManager;

			if (mgr.IsTableStateValid(TableNames.SoundPreCache)) {
				if (SoundIndex >= mgr.Tables[TableNames.SoundPreCache].Entries.Count)
					DemoRef.LogError($"{GetType().Name}: sound index {SoundIndex} out of range");
				else if (SoundIndex != 0)
					SoundName = mgr.Tables[TableNames.SoundPreCache].Entries[SoundIndex].Name;
			}
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.Append(SoundName != null
				? $"sound: {SoundName}"
				: "sound index:");

			pw.Append($" [{SoundIndex}]");
		}
	}
}
