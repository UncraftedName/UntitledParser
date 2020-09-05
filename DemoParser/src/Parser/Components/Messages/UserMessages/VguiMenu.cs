using System;
using System.Collections.Generic;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages.UserMessages {
	
	public class VguiMenu : UserMessage {

		public string Message;
		public bool Show;
		public List<KeyValuePair<string, string>> KeyValues;
		
		
		public VguiMenu(SourceDemo? demoRef) : base(demoRef) {}


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


		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void AppendToWriter(IndentedWriter iw) {
			iw.AppendLine("message: " + Message);
			iw.AppendLine("show: " + Show);
			if (KeyValues.Count > 0) {
				iw.Append($"{KeyValues.Count} key value pair{(KeyValues.Count > 1 ? "s" : "")}: ");
				iw.FutureIndent++;
				foreach (var kv in KeyValues)
					iw.Append($"\n{{{kv.Key}, {kv.Value}}}");
			} else {
				iw.Append("no key value pairs");
			}
		}
	}
}