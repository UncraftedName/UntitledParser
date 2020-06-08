using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {
	
	// disconnect, last message in connection
	public class NetDisconnect : DemoMessage {

		public string Text;
		
		
		public NetDisconnect(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			Text = bsr.ReadNullTerminatedString(); // something ain't right, might be char array of len 1024
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new System.NotImplementedException();
		}


		public override void AppendToWriter(IndentedWriter iw) {
			iw.Append($"text: {Text}");
		}
	}
}