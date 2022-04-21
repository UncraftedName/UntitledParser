using System.Collections.Generic;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages.UserMessages {

	public class VguiMenu : UserMessage {

		public string Message;
		public bool Show;
		public List<KeyValuePair<string, string>> KeyValues;


		public VguiMenu(SourceDemo? demoRef, byte value) : base(demoRef, value) {}


		protected override void Parse(ref BitStreamReader bsr) {
			Message = bsr.ReadNullTerminatedString();
			Show = bsr.ReadByte() != 0;
			int count = bsr.ReadByte();
			KeyValues = new List<KeyValuePair<string, string>>();
			for (int i = 0; i < count; i++) {
				KeyValues.Add(new KeyValuePair<string, string>(
					bsr.ReadNullTerminatedString(),
					bsr.ReadNullTerminatedString()));
			}
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.AppendLine("message: " + Message);
			pw.AppendLine("show: " + Show);
			if (KeyValues.Count > 0) {
				pw.Append($"{KeyValues.Count} key value pair{(KeyValues.Count > 1 ? "s" : "")}: ");
				pw.FutureIndent++;
				foreach (var kv in KeyValues)
					pw.Append($"\n{{{kv.Key}, {kv.Value}}}");
				pw.FutureIndent--;
			} else {
				pw.Append("no key value pairs");
			}
		}
	}
}
