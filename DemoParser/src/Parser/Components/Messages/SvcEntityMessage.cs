using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {
	
	public class SvcEntityMessage : DemoMessage {

		public uint EntityIndex;
		public uint ClassId;
		private BitStreamReader _data; // todo
		public BitStreamReader Data => _data.FromBeginning();
		

		public SvcEntityMessage(SourceDemo? demoRef) : base(demoRef) {}


		protected override void Parse(ref BitStreamReader bsr) {
			EntityIndex = bsr.ReadBitsAsUInt(DemoInfo.MaxEdictBits);
			ClassId = bsr.ReadBitsAsUInt(9);
			_data = bsr.Split(bsr.ReadBitsAsUInt(11));
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void PrettyWrite(IPrettyWriter iw) {
			iw.AppendLine($"entity index: {EntityIndex}");
			iw.AppendLine($"class ID: {ClassId}");
			iw.Append($"data length in bits: {_data.BitLength}");
		}
	}
}