using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Packets.CustomDataTypes {

	public class MenuCallBack : CustomDataMessage {

		public int Unknown;
		public string CallbackStr;


		public MenuCallBack(SourceDemo? demoRef) : base(demoRef) {}


		protected override void Parse(ref BitStreamReader bsr) {
			Unknown = bsr.ReadSInt();
			CallbackStr = bsr.ReadNullTerminatedString();
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.AppendLine($"unknown: {Unknown}");
			pw.Append($"callback string: {CallbackStr}");
		}
	}
}
