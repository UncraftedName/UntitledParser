using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {

	public class SvcSplitScreen : DemoMessage {

		public bool RemoveUser;
		private BitStreamReader _data;
		public BitStreamReader Data => _data.FromBeginning(); // todo


		public SvcSplitScreen(SourceDemo? demoRef, byte value) : base(demoRef, value) {}


		protected override void Parse(ref BitStreamReader bsr) {
			RemoveUser = bsr.ReadBool();
			uint dataLen = bsr.ReadUInt(11);
			_data = bsr.ForkAndSkip((int)dataLen);
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.AppendLine(RemoveUser ? "remove user" : "add user");
			pw.Append($"data of length {Data.BitLength}");
		}
	}
}
