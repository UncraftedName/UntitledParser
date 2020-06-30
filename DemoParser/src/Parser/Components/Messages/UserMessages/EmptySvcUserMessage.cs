using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages.UserMessages {
	
	public class EmptySvcUserMessage : SvcUserMessage {
		
		public override bool MayContainData => false;

		public EmptySvcUserMessage(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		internal override void ParseStream(BitStreamReader bsr) {}

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {}
	}
}