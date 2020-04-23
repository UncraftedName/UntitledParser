using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {
	
	public class SvcEntityMessage : DemoMessage {

		public uint EntityIndex;
		public uint ClassId;
		private BitStreamReader _data;
		public BitStreamReader Data => _data.FromBeginning();
		

		public SvcEntityMessage(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			EntityIndex = bsr.ReadBitsAsUInt(11);
			ClassId = bsr.ReadBitsAsUInt(9);
			_data = bsr.SubStream(bsr.ReadBitsAsUInt(11));
			SetLocalStreamEnd(bsr);
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new System.NotImplementedException();
		}


		public override void AppendToWriter(IndentedWriter iw) {
			iw.AppendLine($"entity index: {EntityIndex}");
			iw.AppendLine($"class ID: {ClassId}");
			iw.Append($"data length in bits: {_data.BitLength}");
		}
	}
}