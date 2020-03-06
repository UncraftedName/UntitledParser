using UntitledParser.Parser.Components.Abstract;
using UntitledParser.Utils;

namespace UntitledParser.Parser.Components.Messages.UserMessages {
	
	public class AchievementEvent : SvcUserMessage {

		public int AchievementID; // https://nekzor.github.io/portal2/achievements
		
		
		public AchievementEvent(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			throw new System.NotImplementedException(); // todo, and figure out the achievements
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new System.NotImplementedException();
		}


		internal override void AppendToWriter(IndentedWriter iw) {
			iw += $"achievement ID: {AchievementID}";
		}
	}
}