using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages.UserMessages {

	public class VoiceMask : UserMessage {

		public const int VoiceMaxPlayers = 2; // might be different for different games, should be 2 for p1 & p2
		public PlayerMask[] PlayerMasks;
		public bool PlayerModEnable;


		public VoiceMask(SourceDemo? demoRef, byte value) : base(demoRef, value) {}


		protected override void Parse(ref BitStreamReader bsr) {
			PlayerMasks = new PlayerMask[VoiceMaxPlayers];
			for (int i = 0; i < VoiceMaxPlayers; i++)
				PlayerMasks[i] = new PlayerMask {GameRulesMask = bsr.ReadSInt(), BanMask = bsr.ReadSInt()};
			PlayerModEnable = bsr.ReadByte() != 0;
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.Append("player masks:");
			pw.FutureIndent++;
			for (int i = 0; i < PlayerMasks.Length; i++) {
				pw.AppendLine();
				pw.AppendLine($"game rules mask #{i + 1}: {PlayerMasks[i].GameRulesMask}");
				pw.Append($"ban mask #{i + 1}: {PlayerMasks[i].BanMask}");
			}
			pw.FutureIndent--;
			pw.AppendLine();
			pw.Append($"player mod enable: {PlayerModEnable}");
		}


		public struct PlayerMask {
			public int GameRulesMask;
			public int BanMask;
		}
	}
}
