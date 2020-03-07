using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;

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


		internal override void AppendToWriter(IndentedWriter iw) {
			if (DemoRef.CStringTablesManager.Readable)
				iw.Append(SoundIndex < DemoRef.CStringTablesManager.SoundTable.Entries.Count
					? $"sound: {DemoRef.CStringTablesManager.SoundTable.Entries[SoundIndex].EntryName}"
					: "sound index (out of range):");
			else
				iw.Append("sound index:");
			
			iw.Append($" ({SoundIndex})");
		}
	}
}