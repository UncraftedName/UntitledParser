using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;

namespace DemoParser.Parser.Components.Messages {
	
	public class SvcMenu : DemoMessage {

		public ushort MenuType;
		private BitStreamReader _data;
		public BitStreamReader Data => _data.FromBeginning();
		

		public SvcMenu(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			MenuType = bsr.ReadUShort();
			uint dataLen = bsr.ReadUInt();
			_data = bsr.SubStream(dataLen);
			bsr.SkipBits(dataLen);
			SetLocalStreamEnd(bsr);
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new System.NotImplementedException();
		}


		internal override void AppendToWriter(IndentedWriter iw) {
			iw.AppendLine($"menu type: {MenuType}");
			iw.Append($"data length in bits: {_data.BitLength}");
		}
	}
}