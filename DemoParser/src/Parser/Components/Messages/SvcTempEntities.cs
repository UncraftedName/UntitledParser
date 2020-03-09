using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;

namespace DemoParser.Parser.Components.Messages {
	
	public class SvcTempEntities : DemoMessage {

		public byte EntryCount;
		private BitStreamReader _data;
		public BitStreamReader Data => _data.FromBeginning();
		public bool? Unknown;
		

		public SvcTempEntities(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			EntryCount = bsr.ReadByte();
			uint bitLen = bsr.ReadBitsAsUInt(17);
			_data = bsr.SubStream(bitLen);
			bsr.SkipBits(bitLen);
			if (DemoRef.DemoSettings.Game == SourceDemoSettings.SourceGame.L4D2_2000)
				Unknown = bsr.ReadBool();
			SetLocalStreamEnd(bsr);
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new System.NotImplementedException();
		}


		internal override void AppendToWriter(IndentedWriter iw) {
			iw.AppendLine($"entry count: {EntryCount}");
			iw.Append($"length in bits: {_data.BitLength}");
			if (Unknown.HasValue)
				iw.Append($"\nunknown: {Unknown}");
		}
	}
}