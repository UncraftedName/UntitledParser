using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {
	
	public class SvcPrint : DemoMessage {

		public string Str;
		
		
		public SvcPrint(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			Str = bsr.ReadNullTerminatedString();
			SetLocalStreamEnd(bsr);
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void AppendToWriter(IndentedWriter iw) {
			string trimmed = Str.Trim();
			iw.Append("string: ");
			if (trimmed.Contains("\n")) {
				iw.FutureIndent++;
				iw.AppendLine();
				iw.Append(trimmed);
				iw.FutureIndent--;
			} else {
				iw.Append(trimmed);
			}
		}
	}
}