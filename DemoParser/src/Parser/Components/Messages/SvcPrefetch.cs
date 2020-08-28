using System;
using System.Collections.Generic;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.HelperClasses;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {
	
	public class SvcPrefetch : DemoMessage {

		public int SoundIndex;
		public string? SoundName;
		
		
		public SvcPrefetch(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			SoundIndex = (int)bsr.ReadBitsAsUInt(13);
			
			var mgr = DemoRef.CurStringTablesManager;

			if (mgr.TableReadable.GetValueOrDefault(TableNames.SoundPreCache)) {
				if (SoundIndex >= mgr.Tables[TableNames.SoundPreCache].Entries.Count)
					DemoRef.LogError($"{GetType().Name} - sound index out of range: {SoundIndex}");
				else if (SoundIndex != 0)
					SoundName = mgr.Tables[TableNames.SoundPreCache].Entries[SoundIndex].EntryName;
			}
			
			SetLocalStreamEnd(bsr);
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void AppendToWriter(IndentedWriter iw) {
			iw.Append(SoundName != null 
				? $"sound: {SoundName}" 
				: "sound index:");
			
			iw.Append($" ({SoundIndex})");
		}
	}
}