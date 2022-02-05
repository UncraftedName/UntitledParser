using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {

	public class SvcSplitScreen : DemoMessage {

		public bool RemoveUser;
		public BitStreamReader Data {get;private set;}


		public SvcSplitScreen(SourceDemo? demoRef) : base(demoRef) {}


		protected override void Parse(ref BitStreamReader bsr) {
			RemoveUser = bsr.ReadBool();
			uint dataLen = bsr.ReadUInt(11);
			Data = bsr.SplitAndSkip(dataLen);
		}


		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.AppendLine(RemoveUser ? "remove user" : "add user");
			pw.Append($"data of length {Data.BitLength}");
		}
	}
}
