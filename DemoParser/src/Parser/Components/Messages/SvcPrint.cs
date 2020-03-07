using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;

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


		internal override void AppendToWriter(IndentedWriter iw) {
			iw.Append("string: ");
			iw.AddIndent();
			iw.Append(Str.TrimEnd());
			iw.SubIndent();
		}
	}
}