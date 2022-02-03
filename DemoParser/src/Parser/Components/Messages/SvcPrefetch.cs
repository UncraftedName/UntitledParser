using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.HelperClasses;
using DemoParser.Parser.HelperClasses.GameState;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {

	public class SvcPrefetch : DemoMessage {

		public int SoundIndex;
		public string? SoundName;


		public SvcPrefetch(SourceDemo? demoRef) : base(demoRef) {}


		protected override void Parse(ref BitStreamReader bsr) {
			var soundIndexBits = DemoInfo.IsLeft4Dead2() ? DemoInfo.Game >= SourceGame.L4D2_2091 ? 15 : 14 : 13;
			SoundIndex = (int)bsr.ReadUInt(soundIndexBits);

			var mgr = DemoRef.StringTablesManager;

			if (mgr.TableReadable.GetValueOrDefault(TableNames.SoundPreCache)) {
				if (SoundIndex >= mgr.Tables[TableNames.SoundPreCache].Entries.Count)
					DemoRef.LogError($"{GetType().Name} - sound index out of range: {SoundIndex}");
				else if (SoundIndex != 0)
					SoundName = mgr.Tables[TableNames.SoundPreCache].Entries[SoundIndex].EntryName;
			}
		}


		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.Append(SoundName != null
				? $"sound: {SoundName}"
				: "sound index:");

			pw.Append($" [{SoundIndex}]");
		}
	}
}
