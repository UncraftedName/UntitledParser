using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;

namespace DemoParser.Parser.Components.Messages {
	
	// disconnect, last message in connection
	public class NetDisconnect : DemoMessage {

		public string Text;
		
		
		public NetDisconnect(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			Text = bsr.ReadNullTerminatedString(); // something ain't right, might be char array 0f len 1024
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new System.NotImplementedException();
		}


		internal override void AppendToWriter(IndentedWriter iw) {
			iw += $"text: {Text}";
		}
	}
}