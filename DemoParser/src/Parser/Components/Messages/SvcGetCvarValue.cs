using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;

namespace DemoParser.Parser.Components.Messages {
	
	public class SvcGetCvarValue : DemoMessage {

		public int Cookie;
		public string CvarName;
		
		
		public SvcGetCvarValue(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			Cookie = bsr.ReadSInt();
			CvarName = bsr.ReadNullTerminatedString();
			SetLocalStreamEnd(bsr);
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new System.NotImplementedException();
		}


		internal override void AppendToWriter(IndentedWriter iw) {
			iw.AppendLine($"cookie: {Cookie}");
			iw.Append($"cvar name: {CvarName}");
		}
	}
}