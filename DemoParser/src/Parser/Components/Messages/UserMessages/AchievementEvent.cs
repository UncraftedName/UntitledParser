using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages.UserMessages {
	
	public class AchievementEvent : UserMessage {

		public int AchievementId; // https://nekzor.github.io/portal2/achievements
		
		
		public AchievementEvent(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			throw new NotImplementedException(); // todo, and figure out the achievements
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void AppendToWriter(IndentedWriter iw) {
			iw.Append($"achievement ID: {AchievementId}");
		}
	}
}