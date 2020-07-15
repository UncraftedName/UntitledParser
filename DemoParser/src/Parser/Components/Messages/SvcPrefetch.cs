using System.Collections.Generic;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.HelperClasses;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {
	
	public class SvcPrefetch : DemoMessage {

		public int SoundIndex;
		
		
		public SvcPrefetch(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			SoundIndex = (int)bsr.ReadBitsAsUInt(13);
			SetLocalStreamEnd(bsr);
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new System.NotImplementedException();
		}


		public override void AppendToWriter(IndentedWriter iw) {
			var mgr = DemoRef.CurStringTablesManager;
			if (mgr.TableReadable.GetValueOrDefault(TableNames.SoundPreCache))
				iw.Append(SoundIndex < mgr.Tables[TableNames.SoundPreCache].Entries.Count
					? $"sound: {mgr.Tables[TableNames.SoundPreCache].Entries[SoundIndex].EntryName}"
					: "sound index (out of range):");
			else
				iw.Append("sound index:");
			
			iw.Append($" ({SoundIndex})");
		}
	}
}