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
			throw new System.NotImplementedException();
		}


		public override void AppendToWriter(IndentedWriter iw) {
			iw.Append("string: ");
			iw.FutureIndent++;
			iw.Append(Str.TrimEnd());
			iw.FutureIndent--;
		}
	}
}