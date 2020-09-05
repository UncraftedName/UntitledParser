using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages.UserMessages {
	
	public class EmptyUserMessage : UserMessage {
		
		public override bool MayContainData => false;

		public EmptyUserMessage(SourceDemo? demoRef) : base(demoRef) {}

		protected override void Parse(ref BitStreamReader bsr) {}

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {}
	}
}