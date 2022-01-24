using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {

	public class SvcTempEntities : DemoMessage {

		public byte EntryCount;
		private BitStreamReader _data;
		public BitStreamReader Data => _data.FromBeginning();

		public SvcTempEntities(SourceDemo? demoRef) : base(demoRef) {}


		protected override void Parse(ref BitStreamReader bsr) {
			EntryCount = bsr.ReadByte();
			uint bitLen = bsr.ReadBitsAsUInt(DemoInfo.IsLeft4Dead2() ? 18 : 17);
			_data = bsr.SplitAndSkip(bitLen);

			// todo
		}


		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.AppendLine($"entry count: {EntryCount}");
			pw.Append($"length in bits: {_data.BitLength}");
		}
	}
}
