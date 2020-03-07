using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;

namespace DemoParser.Parser.Components.Messages.UserMessages {
	
	public class LogoTimeMsg : SvcUserMessage {

		public float Time;
		
		
		public LogoTimeMsg(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			Time = bsr.ReadFloat();
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new System.NotImplementedException();
		}


		internal override void AppendToWriter(IndentedWriter iw) {
			iw += $"time: {Time}";
		}
	}
}