using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {

	public class SvcMenu : DemoMessage {

		public ushort MenuType;
		private BitStreamReader _data;
		public BitStreamReader Data => _data.FromBeginning(); // todo


		public SvcMenu(SourceDemo? demoRef) : base(demoRef) {}


		protected override void Parse(ref BitStreamReader bsr) {
			MenuType = bsr.ReadUShort();
			uint dataLen = bsr.ReadUInt();
			_data = bsr.SplitAndSkip(dataLen);
		}


		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.AppendLine($"menu type: {MenuType}");
			pw.Append($"data length in bits: {_data.BitLength}");
		}
	}
}
