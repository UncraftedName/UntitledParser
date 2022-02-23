using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages.UserMessages {

	public class AchievementEvent : UserMessage {

		public int AchievementId; // https://nekzor.github.io/portal2/achievements


		public AchievementEvent(SourceDemo? demoRef, byte value) : base(demoRef, value) {}


		protected override void Parse(ref BitStreamReader bsr) {
			throw new NotImplementedException(); // todo, and figure out the achievements
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.Append($"achievement ID: {AchievementId}");
		}
	}
}
