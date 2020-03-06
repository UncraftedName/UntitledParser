using System;
using System.Diagnostics.CodeAnalysis;
using UntitledParser.Parser.Components.Messages.UserMessages;
using UntitledParser.Utils;
using static UntitledParser.Parser.SourceDemoSettings.SourceGame;

// this is a two way dict so that i can map the byte to the message type and also know the reverse for debugging and for writing the demo
using TypeReMapper = UntitledParser.Utils.TwoWayDict<byte, UntitledParser.Parser.Components.Abstract.UserMessageType>;


namespace UntitledParser.Parser.Components.Abstract {
	
	/* The idea here is that the enum corresponding to user messages is very shuffled/shifted in different games,
	 * and the usermessages seem to be a byte number of bits long, with every byte of the data being used.
	 * Therefore, if each byte isn't read, then I know that the message is either being read wrong, or I'm reading the wrong user message.
	 * Instead of using a separate enums for different games, I have a single enum (for portal 1 leak) and many lookup tables for different games.
	 * And during parsing, if not every byte is read, then I revert to an unknown message type, log an error,
	 * and the unknown message type will simply print the contents of the message in hex (when converting to string).
	 * The lookup tables are just me guessing for the most part.
	 */
	
	public abstract class SvcUserMessage : DemoComponent {

		#region lookup tables

		// these are mostly offset by one from the leak, so many that i don't technically know for sure tho
		private static readonly TypeReMapper Portal1ReMapper = new TypeReMapper {
			[30] = UserMessageType.KillCam, // idk if this is correct
			[33] = UserMessageType.LogoTimeMsg // not sure what 34 is yet. 6 bytes, almost def has only int field(s)
		};
		private static readonly TypeReMapper Portal1SteamPipe = new TypeReMapper {};
		private static readonly TypeReMapper L4D2ReMapper = new TypeReMapper {};
		// 48 is something related to shooting/failing to place portals
		// 59 is spammed in one packet, seems to be when entering disassembly machine
		private static readonly TypeReMapper Portal2ReMapper = new TypeReMapper {};
		
		#endregion

		protected SvcUserMessage(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}


		public static UserMessageType ByteToUserMessageType(SourceDemo demoRef, byte b) {
			var def = (UserMessageType)b;
			return demoRef.DemoSettings.Game switch {
				PORTAL_1 			=> Portal1ReMapper.GetValueOrDefault(b, def),
				PORTAL_1_STEAMPIPE 	=> Portal1SteamPipe.GetValueOrDefault(b, def),
				L4D2_2000 			=> L4D2ReMapper.GetValueOrDefault(b, def),
				PORTAL_2 			=> Portal2ReMapper.GetValueOrDefault(b, def),
				_ => def
			};
		}


		public static byte UserMessageTypeToByte(SourceDemo demoRef, UserMessageType m) {
			var def = (byte)m;
			return demoRef.DemoSettings.Game switch {
				PORTAL_1 			=> Portal1ReMapper.GetValueOrDefault(m, def),
				PORTAL_1_STEAMPIPE 	=> Portal1SteamPipe.GetValueOrDefault(m, def),
				L4D2_2000 			=> L4D2ReMapper.GetValueOrDefault(m, def),
				PORTAL_2 			=> Portal2ReMapper.GetValueOrDefault(m, def),
				_ => def
			};
		}
	}
	

	public static class SvcUserMessageFactory {
		// make sure to pass in a substream
		public static SvcUserMessage CreateUserMessage(SourceDemo demoRef, BitStreamReader reader, UserMessageType messageType) {
			SvcUserMessage msgResult = messageType switch {
				UserMessageType.CloseCaption 		=> new CloseCaption(demoRef, reader),
				UserMessageType.AchievementEvent 	=> new AchievementEvent(demoRef, reader),
				UserMessageType.SayText2 			=> new SayText2(demoRef, reader),
				UserMessageType.SquadMemberDied 	=> new SquadMemberDied(demoRef, reader),
				UserMessageType.GameTitle			=> new GameTitle(demoRef, reader),
				UserMessageType.RequestState 		=> new RequestState(demoRef, reader),
				UserMessageType.Shake 				=> new Shake(demoRef, reader),
				UserMessageType.Fade 				=> new Fade(demoRef, reader),
				UserMessageType.Damage 				=> new Damage(demoRef, reader),
				UserMessageType.Rumble 				=> new Rumble(demoRef, reader),
				UserMessageType.ResetHUD 			=> new ResetHUD(demoRef, reader),
				UserMessageType.LogoTimeMsg 		=> new LogoTimeMsg(demoRef, reader),
				UserMessageType.Battery 			=> new Battery(demoRef, reader),
				UserMessageType.Geiger				=> new Geiger(demoRef, reader),
				UserMessageType.KillCam				=> new KillCam(demoRef, reader),
				UserMessageType.EntityPortalled		=> new EntityPortalled(demoRef, reader),
				UserMessageType.HUDMsg				=> new HudMsg(demoRef, reader),
				UserMessageType.KeyHintText			=> new KeyHintText(demoRef, reader),
				UserMessageType.Train				=> new Train(demoRef, reader),
				_ => null
			};
			
			if (msgResult != null) {
				return msgResult;
			} else {
				#region error logging
					
				bool defined = Enum.IsDefined(typeof(UserMessageType), messageType);
				string s =  defined ? "unimplemented" : "unknown";
				s += $" SvcUserMessage: {messageType}";
				if (defined)
					s += $" ({SvcUserMessage.UserMessageTypeToByte(demoRef, messageType)})";
				demoRef.AddError($"{s} ({ParserTextUtils.PluralIfNotOne(reader.BitsRemaining / 8, "byte", "s")})" +
								 $" - {reader.FromBeginning().ToHexString()}");
					
				#endregion
				return new UnknownSvcUserMessage(demoRef, reader);
			}
		}
	}
	

	// these types are very different and very shuffled depending on the game.
	// for portal it's in src_main/game/shared/portal/portal_usermessages.cpp
	// for other sdk's it's in similar locations, the csgo values can be found at
	// https://github.com/StatsHelix/demoinfo/blob/ac3e820d68a5a76b1c4c86bf3951e9799f669a56/DemoInfo/Messages/EnumConstants.cs
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	public enum UserMessageType : byte {
		Geiger = 0,
		Train,
		HudText,
		SayText,
		SayText2,
		TextMsg,    //one of the messages here doesn't actually exist in the leak if tho it registers??
		HUDMsg,
		ResetHUD, 		// called every respawn
		GameTitle, 		// show game title
		ItemPickup, 	// for item history on screen
		ShowMenu,
		Shake,
		Fade,
		VGUIMenu,
		Rumble, 		// Send a rumble to a controller
		Battery,
		Damage, 		// for HUD damage indicators
		VoiceMask,
		RequestState,
		CloseCaption,
		HintText,  
		KeyHintText,
		SquadMemberDied,
		AmmoDenied,
		CreditsMessage,
		CreditsPortalMessage,
		LogoTimeMsg,
		AchievementEvent,
		EntityPortalled,
		KillCam
	} 
}