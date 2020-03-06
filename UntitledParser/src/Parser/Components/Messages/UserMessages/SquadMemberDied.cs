using UntitledParser.Parser.Components.Abstract;
using UntitledParser.Utils;

namespace UntitledParser.Parser.Components.Messages.UserMessages {
	
	public class SquadMemberDied : SvcUserMessage {
		
		public override bool MayContainData => false;
		

		public SquadMemberDied(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new System.NotImplementedException();
		}
	}
}