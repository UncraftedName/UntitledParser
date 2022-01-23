using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {

	public class SvcPrint : DemoMessage {

		public string Str;


		public SvcPrint(SourceDemo? demoRef) : base(demoRef) {}


		protected override void Parse(ref BitStreamReader bsr) {
			Str = bsr.ReadNullTerminatedString();
		}


		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			string trimmed = Str.Trim();
			pw.Append("string: ");
			if (trimmed.Contains("\n")) {
				pw.FutureIndent++;
				pw.AppendLine();
				pw.Append(trimmed);
				pw.FutureIndent--;
			} else {
				pw.Append(trimmed);
			}
		}
	}
}
