using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;

namespace DemoParser.Parser.Components.Messages {
	
	public class SvcSendTable : DemoMessage {

		public bool NeedsDecoder;
		private BitStreamReader _props;
		public BitStreamReader Props => _props.FromBeginning();
		

		public SvcSendTable(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			NeedsDecoder = bsr.ReadBool();
			ushort len = bsr.ReadUShort();
			_props = bsr.SubStream(len);
			SetLocalStreamEnd(bsr);
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new System.NotImplementedException();
		}


		internal override void AppendToWriter(IndentedWriter iw) {
			iw.AppendLine($"needs decoder: {NeedsDecoder}");
			iw.Append($"data length in bits: {_props.BitLength}");
		}
	}
}