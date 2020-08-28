using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages.UserMessages {
	
	public class VoiceMask : UserMessage {
		
		public const int VoiceMaxPlayers = 2; // might be different for different games, should be 2 for p1 & p2
		public PlayerMask[] PlayerMasks;
		public bool PlayerModEnable;
		
		
		public VoiceMask(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			PlayerMasks = new PlayerMask[VoiceMaxPlayers];
			for (int i = 0; i < VoiceMaxPlayers; i++)
				PlayerMasks[i] = new PlayerMask {GameRulesMask = bsr.ReadSInt(), BanMask = bsr.ReadSInt()};
			PlayerModEnable = bsr.ReadByte() != 0;
		}
		
		
		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}
		
		
		public override void AppendToWriter(IndentedWriter iw) {
			iw.Append("player masks:");
			iw.FutureIndent++;
			for (int i = 0; i < PlayerMasks.Length; i++) {
				iw.AppendLine();
				iw.AppendLine($"game rules mask #{i + 1}: {PlayerMasks[i].GameRulesMask}");
				iw.Append($"ban mask #{i + 1}: {PlayerMasks[i].BanMask}");
			}
			iw.FutureIndent--;
			iw.AppendLine();
			iw.Append($"player mod enable: {PlayerModEnable}");
		}
		
		
		public struct PlayerMask { // todo
			public int GameRulesMask;
			public int BanMask;
		}
	}
}