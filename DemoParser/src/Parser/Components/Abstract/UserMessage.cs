using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DemoParser.Parser.Components.Messages.UserMessages;
using DemoParser.Parser.Components.Messages.UserMessages.Haptic;

namespace DemoParser.Parser.Components.Abstract {

	/* The idea here is that the enum corresponding to user messages is shuffled/shifted in different games,
	 * and the user messages seem to be a byte number of bits long, with every byte of the data being used.
	 * Therefore, if each byte isn't read, then I know that the message is either being read wrong, or I'm reading
	 * the wrong user message. During parsing, if not every byte is read, then I revert to an unknown message type,
	 * log an error, and the unknown message type will simply print the contents of the message in hex (when converting
	 * this to string). The lookup tables can be obtained from the RegisterUserMessages() function in server.dll.
	 */

	/// <summary>
	/// The payload in SvcUserMessage.
	/// </summary>
	public abstract class UserMessage : DemoComponent {

		#region user message lookup tables for different versions

		public static readonly IReadOnlyList<UserMessageType> Hl2OeTable = new[] {
			UserMessageType.Geiger,
			UserMessageType.Train,
			UserMessageType.HudText,
			UserMessageType.SayText,
			UserMessageType.TextMsg,
			UserMessageType.HUDMsg,
			UserMessageType.ResetHUD,
			UserMessageType.GameTitle,
			UserMessageType.ItemPickup,
			UserMessageType.ShowMenu,
			UserMessageType.Shake,
			UserMessageType.Fade,
			UserMessageType.VGUIMenu,
			UserMessageType.Battery,
			UserMessageType.Damage,
			UserMessageType.VoiceMask,
			UserMessageType.RequestState,
			UserMessageType.CloseCaption,
			UserMessageType.HintText,
			UserMessageType.SquadMemberDied,
			UserMessageType.AmmoDenied,
			UserMessageType.CreditsMsg
		};

		public static readonly IReadOnlyList<UserMessageType> Hl2UnpackTable = new[] {
			UserMessageType.Geiger,
			UserMessageType.Train,
			UserMessageType.HudText,
			UserMessageType.SayText,
			UserMessageType.SayText2,
			UserMessageType.TextMsg,
			UserMessageType.HUDMsg,
			UserMessageType.ResetHUD,
			UserMessageType.GameTitle,
			UserMessageType.ItemPickup,
			UserMessageType.ShowMenu,
			UserMessageType.Shake,
			UserMessageType.Fade,
			UserMessageType.VGUIMenu,
			UserMessageType.Rumble,
			UserMessageType.Battery,
			UserMessageType.Damage,
			UserMessageType.VoiceMask,
			UserMessageType.RequestState,
			UserMessageType.CloseCaption,
			UserMessageType.HintText,
			UserMessageType.KeyHintText,
			UserMessageType.SquadMemberDied,
			UserMessageType.AmmoDenied,
			UserMessageType.CreditsMsg,
			UserMessageType.LogoTimeMsg,
			UserMessageType.AchievementEvent,
			UserMessageType.UpdateJalopyRadar,
			UserMessageType.SPHapWeapEvent,
			UserMessageType.HapDmg,
			UserMessageType.HapPunch,
			UserMessageType.HapSetDrag,
			UserMessageType.HapSetConst,
			UserMessageType.HapMeleeContact
		};

		public static readonly IReadOnlyList<UserMessageType> Portal2Table = new[] {
			UserMessageType.Geiger,
			UserMessageType.Train,
			UserMessageType.HudText,
			UserMessageType.SayText,
			UserMessageType.SayText2,
			UserMessageType.TextMsg,
			UserMessageType.HUDMsg,
			UserMessageType.ResetHUD,
			UserMessageType.GameTitle,
			UserMessageType.ItemPickup,
			UserMessageType.ShowMenu,
			UserMessageType.Shake,
			UserMessageType.Tilt,
			UserMessageType.Fade,
			UserMessageType.VGUIMenu,
			UserMessageType.Rumble,
			UserMessageType.Battery,
			UserMessageType.Damage,
			UserMessageType.VoiceMask,
			UserMessageType.RequestState,
			UserMessageType.CloseCaption,
			UserMessageType.CloseCaptionDirect,
			UserMessageType.HintText,
			UserMessageType.KeyHintText,
			UserMessageType.SquadMemberDied,
			UserMessageType.AmmoDenied,
			UserMessageType.CreditsMsg,
			UserMessageType.LogoTimeMsg,
			UserMessageType.AchievementEvent,
			UserMessageType.UpdateJalopyRadar,
			UserMessageType.CurrentTimescale,
			UserMessageType.DesiredTimescale,
			UserMessageType.InventoryFlash,
			UserMessageType.CreditsPortalMsg,
			UserMessageType.IndicatorFlash,
			UserMessageType.ControlHelperAnimate,
			UserMessageType.TakePhoto,
			UserMessageType.Flash,
			UserMessageType.HudPingIndicator,
			UserMessageType.OpenRadialMenu,
			UserMessageType.AddLocator,
			UserMessageType.MPMapCompleted,
			UserMessageType.MPMapIncomplete,
			UserMessageType.MPMapCompletedData,
			UserMessageType.MPTauntEarned,
			UserMessageType.MPTauntUnlocked,
			UserMessageType.MPTauntLocked,
			UserMessageType.MPAllTauntsLocked,
			UserMessageType.PortalFX_Surface,
			UserMessageType.PaintWorld,
			UserMessageType.PaintEntity,
			UserMessageType.ChangePaintColor,
			UserMessageType.PaintBombExplode,
			UserMessageType.RemoveAllPaint,
			UserMessageType.PaintAllSurfaces,
			UserMessageType.RemovePaint,
			UserMessageType.StartSurvey,
			UserMessageType.ApplyHitBoxDamageEffect,
			UserMessageType.SetMixLayerTriggerFactor,
			UserMessageType.TransitionFade,
			UserMessageType.ScoreboardTempUpdate,
			UserMessageType.ChallengeModCheatSession,
			UserMessageType.ChallengeModCloseAllUI
		};

		public static readonly IReadOnlyList<UserMessageType> Portal3420Table = new[] {
			UserMessageType.Geiger,
			UserMessageType.Train,
			UserMessageType.HudText,
			UserMessageType.SayText,
			UserMessageType.SayText2,
			UserMessageType.TextMsg,
			UserMessageType.HUDMsg,
			UserMessageType.ResetHUD,
			UserMessageType.GameTitle,
			UserMessageType.ItemPickup,
			UserMessageType.ShowMenu,
			UserMessageType.Shake,
			UserMessageType.Fade,
			UserMessageType.VGUIMenu,
			UserMessageType.Rumble,
			UserMessageType.Battery,
			UserMessageType.Damage,
			UserMessageType.VoiceMask,
			UserMessageType.RequestState,
			UserMessageType.CloseCaption,
			UserMessageType.HintText,
			UserMessageType.KeyHintText,
			UserMessageType.SquadMemberDied,
			UserMessageType.AmmoDenied,
			UserMessageType.CreditsMsg,
			UserMessageType.CreditsPortalMsg,
			UserMessageType.LogoTimeMsg,
			UserMessageType.AchievementEvent,
			UserMessageType.EntityPortalled,
			UserMessageType.KillCam
		};

		public static readonly IReadOnlyList<UserMessageType> Portal5135Table = new[] {
			UserMessageType.Geiger,
			UserMessageType.Train,
			UserMessageType.HudText,
			UserMessageType.SayText,
			UserMessageType.SayText2,
			UserMessageType.TextMsg,
			UserMessageType.HUDMsg,
			UserMessageType.ResetHUD,
			UserMessageType.GameTitle,
			UserMessageType.ItemPickup,
			UserMessageType.ShowMenu,
			UserMessageType.Shake,
			UserMessageType.Fade,
			UserMessageType.VGUIMenu,
			UserMessageType.Rumble,
			UserMessageType.Battery,
			UserMessageType.Damage,
			UserMessageType.VoiceMask,
			UserMessageType.RequestState,
			UserMessageType.CloseCaption,
			UserMessageType.HintText,
			UserMessageType.KeyHintText,
			UserMessageType.SquadMemberDied,
			UserMessageType.AmmoDenied,
			UserMessageType.CreditsMsg,
			UserMessageType.CreditsPortalMsg,
			UserMessageType.LogoTimeMsg,
			UserMessageType.AchievementEvent,
			UserMessageType.EntityPortalled,
			UserMessageType.KillCam,
			UserMessageType.SPHapWeapEvent,
			UserMessageType.HapDmg,
			UserMessageType.HapPunch,
			UserMessageType.HapSetDrag,
			UserMessageType.HapSetConst,
			UserMessageType.HapMeleeContact
		};

		public static readonly IReadOnlyList<UserMessageType> Portal1SteamTable = new[] {
			UserMessageType.Geiger,
			UserMessageType.Train,
			UserMessageType.HudText,
			UserMessageType.SayText,
			UserMessageType.SayText2,
			UserMessageType.TextMsg,
			UserMessageType.HUDMsg,
			UserMessageType.ResetHUD,
			UserMessageType.GameTitle,
			UserMessageType.ItemPickup,
			UserMessageType.ShowMenu,
			UserMessageType.Shake,
			UserMessageType.Fade,
			UserMessageType.VGUIMenu,
			UserMessageType.Rumble,
			UserMessageType.Battery,
			UserMessageType.Damage,
			UserMessageType.VoiceMask,
			UserMessageType.RequestState,
			UserMessageType.CloseCaption,
			UserMessageType.HintText,
			UserMessageType.KeyHintText,
			UserMessageType.SquadMemberDied,
			UserMessageType.AmmoDenied,
			UserMessageType.CreditsMsg,
			UserMessageType.CreditsPortalMsg,
			UserMessageType.LogoTimeMsg,
			UserMessageType.AchievementEvent,
			UserMessageType.EntityPortalled,
			UserMessageType.KillCam,
			UserMessageType.CallVoteFailed,
			UserMessageType.VoteStart,
			UserMessageType.VotePass,
			UserMessageType.VoteFailed,
			UserMessageType.VoteSetup,
			UserMessageType.SPHapWeapEvent,
			UserMessageType.HapDmg,
			UserMessageType.HapPunch,
			UserMessageType.HapSetDrag,
			UserMessageType.HapSetConst,
			UserMessageType.HapMeleeContact
		};

		public static readonly IReadOnlyList<UserMessageType> L4D2SteamTable = new[] {
			UserMessageType.Geiger,
			UserMessageType.Train,
			UserMessageType.HudText,
			UserMessageType.SayText,
			UserMessageType.SayText2,
			UserMessageType.TextMsg,
			UserMessageType.HUDMsg,
			UserMessageType.ResetHUD,
			UserMessageType.GameTitle,
			UserMessageType.ItemPickup,
			UserMessageType.ShowMenu,
			UserMessageType.Shake,
			UserMessageType.Fade,
			UserMessageType.VGUIMenu,
			UserMessageType.Rumble,
			UserMessageType.CloseCaption,
			UserMessageType.CloseCaptionDirect,
			UserMessageType.SendAudio,
			UserMessageType.RawAudio,
			UserMessageType.VoiceMask,
			UserMessageType.RequestState,
			UserMessageType.BarTime,
			UserMessageType.Damage,
			UserMessageType.RadioText,
			UserMessageType.HintText,
			UserMessageType.KeyHintText,
			UserMessageType.ReloadEffect,
			UserMessageType.PlayerAnimEvent,
			UserMessageType.AmmoDenied,
			UserMessageType.UpdateRadar,
			UserMessageType.KillCam,
			UserMessageType.MarkAchievement,
			UserMessageType.Splatter,
			UserMessageType.MeleeSlashSplatter,
			UserMessageType.MeleeClubSplatter,
			UserMessageType.MudSplatter,
			UserMessageType.SplatterClear,
			UserMessageType.MessageText,
			UserMessageType.TransitionRestore,
			UserMessageType.Spawn,
			UserMessageType.CreditsLine,
			UserMessageType.CreditsMsg,
			UserMessageType.JoinLateMsg,
			UserMessageType.StatsCrawlMsg,
			UserMessageType.StatsSkipState,
			UserMessageType.ShowStats,
			UserMessageType.BlurFade,
			UserMessageType.MusicCmd,
			UserMessageType.WitchBloodSplatter,
			UserMessageType.AchievementEvent,
			UserMessageType.PZDmgMsg,
			UserMessageType.AllPlayersConnectedGameStarting,
			UserMessageType.VoteRegistered,
			UserMessageType.DisconnectToLobby,
			UserMessageType.CallVoteFailed,
			UserMessageType.SteamWeaponStatData,
			UserMessageType.CurrentTimescale,
			UserMessageType.DesiredTimescale,
			UserMessageType.PZEndGamePanelMsg,
			UserMessageType.PZEndGamePanelVoteRegisteredMsg,
			UserMessageType.PZEndGameVoteStatsMsg,
			UserMessageType.VoteStart,
			UserMessageType.VotePass,
			UserMessageType.VoteFailed // called VoteFail in l4d2
		};

		// all but the last 3 (the vote uMessages)
		public static readonly IReadOnlyList<UserMessageType> L4D2OldSoloTable = L4D2SteamTable.Take(61).ToArray();

		public static readonly IReadOnlyList<UserMessageType> L4D1OldTable = new[]
		{
			UserMessageType.Geiger,
			UserMessageType.Train,
			UserMessageType.HudText,
			UserMessageType.SayText,
			UserMessageType.SayText2,
			UserMessageType.TextMsg,
			UserMessageType.HUDMsg,
			UserMessageType.ResetHUD,
			UserMessageType.GameTitle,
			UserMessageType.ItemPickup,
			UserMessageType.ShowMenu,
			UserMessageType.Shake,
			UserMessageType.Fade,
			UserMessageType.VGUIMenu,
			UserMessageType.Rumble,
			UserMessageType.CloseCaption,
			UserMessageType.SendAudio,
			UserMessageType.RawAudio,
			UserMessageType.VoiceMask,
			UserMessageType.RequestState,
			UserMessageType.BarTime,
			UserMessageType.Damage,
			UserMessageType.RadioText,
			UserMessageType.HintText,
			UserMessageType.KeyHintText,
			UserMessageType.ReloadEffect,
			UserMessageType.PlayerAnimEvent,
			UserMessageType.AmmoDenied,
			UserMessageType.UpdateRadar,
			UserMessageType.KillCam,
			UserMessageType.MarkAchievement,
			UserMessageType.Splatter,
			UserMessageType.SplatterClear,
			UserMessageType.MessageText,
			UserMessageType.TransitionRestore,
			UserMessageType.Spawn,
			UserMessageType.CreditsLine,
			UserMessageType.CreditsMsg,
			UserMessageType.StatsCrawlMsg,
			UserMessageType.StatsSkipState,
			UserMessageType.ShowStats,
			UserMessageType.MusicCmd,
			UserMessageType.WitchBloodSplatter,
			UserMessageType.AchievementEvent,
			UserMessageType.PZDmgMsg,
			UserMessageType.HideLoadingPlaque,
			UserMessageType.VoteRegistered,
			UserMessageType.DisconnectToLobby,
			UserMessageType.CallVoteFailed
		};

		#endregion

		protected UserMessage(SourceDemo? demoRef) : base(demoRef) {}


		public static UserMessageType ByteToUserMessageType(DemoInfo demoInfo, byte b) {
			var tab = demoInfo.UserMessageTypes;
			if (tab == null)
				return UserMessageType.Unknown;
			else if (b >= tab.Count)
				return UserMessageType.Invalid;
			else
				return tab[b];
		}


		public static byte UserMessageTypeToByte(DemoInfo demoInfo, UserMessageType m) {
			if (demoInfo.UserMessageTypesReverseLookup.TryGetValue(m, out int i))
				return (byte)i;
			throw new ArgumentException($"no user message found for {m}");
		}
	}


	public static class SvcUserMessageFactory {

		private static readonly ImmutableHashSet<UserMessageType> EmptyMessages = new[] {
			UserMessageType.GameTitle,
			UserMessageType.RequestState,
			UserMessageType.SquadMemberDied,
			UserMessageType.RemoveAllPaint,
			UserMessageType.SplatterClear,
			UserMessageType.TransitionRestore,
			UserMessageType.CreditsMsg,
			UserMessageType.JoinLateMsg,
			UserMessageType.StatsCrawlMsg,
			UserMessageType.BlurFade,
			UserMessageType.AllPlayersConnectedGameStarting,
			UserMessageType.DisconnectToLobby,
			UserMessageType.HapMeleeContact,
			UserMessageType.HideLoadingPlaque
		}.ToImmutableHashSet();


		public static UserMessage? CreateUserMessage(
			SourceDemo? demoRef,
			UserMessageType messageType)
		{

			if (EmptyMessages.Contains(messageType))
				return new EmptyUserMessage(demoRef);

			return messageType switch {
				UserMessageType.CloseCaption       => new CloseCaption(demoRef),
				UserMessageType.AchievementEvent   => new AchievementEvent(demoRef),
				UserMessageType.SayText2           => new SayText2(demoRef),
				UserMessageType.Shake              => new Shake(demoRef),
				UserMessageType.Fade               => new Fade(demoRef),
				UserMessageType.Damage             => new Damage(demoRef),
				UserMessageType.Rumble             => new Rumble(demoRef),
				UserMessageType.ResetHUD           => new ResetHUD(demoRef),
				UserMessageType.LogoTimeMsg        => new LogoTimeMsg(demoRef),
				UserMessageType.Battery            => new Battery(demoRef),
				UserMessageType.Geiger             => new Geiger(demoRef),
				UserMessageType.KillCam            => new KillCam(demoRef),
				UserMessageType.EntityPortalled    => new EntityPortalled(demoRef),
				UserMessageType.HUDMsg             => new HudMsg(demoRef),
				UserMessageType.KeyHintText        => new KeyHintText(demoRef),
				UserMessageType.Train              => new Train(demoRef),
				UserMessageType.VGUIMenu           => new VguiMenu(demoRef),
				UserMessageType.TextMsg            => new TextMsg(demoRef),
				UserMessageType.PortalFX_Surface   => new PortalFxSurface(demoRef),
				UserMessageType.VoiceMask          => new VoiceMask(demoRef),
				UserMessageType.SayText            => new SayText(demoRef),
				UserMessageType.MPMapCompleted     => new MpMapCompleted(demoRef),
				UserMessageType.MPMapIncomplete    => new MpMapIncomplete(demoRef),
				UserMessageType.MPMapCompletedData => new MpMapCompletedData(demoRef),
				UserMessageType.MPTauntEarned      => new MpTauntEarned(demoRef),
				UserMessageType.MPTauntLocked      => new MpTauntLocked(demoRef),
				UserMessageType.HudText            => new HudText(demoRef),
				UserMessageType.PaintWorld         => new PaintWorld(demoRef),
				UserMessageType.PaintEntity        => new PaintEntity(demoRef),
				UserMessageType.HapSetConst        => new HapSetConstForce(demoRef),
				UserMessageType.HapSetDrag         => new HapSetDrag(demoRef),
				UserMessageType.HapPunch           => new HapPunch(demoRef),
				UserMessageType.SPHapWeapEvent     => new SpHapWeaponEvent(demoRef),
				_ => null // I do a check for this so that I don't have to allocate the unknown type twice
			};
		}
	}


	// These types may be different and shuffled depending on the game.
	// For portal it's in src_main/game/shared/portal/portal_usermessages.cpp.
	// For other sdk's it's in similar locations, the csgo values can be found at
	// https://github.com/StatsHelix/demoinfo/blob/ac3e820d68a5a76b1c4c86bf3951e9799f669a56/DemoInfo/Messages/EnumConstants.cs
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	[SuppressMessage("ReSharper", "IdentifierTypo")]
	public enum UserMessageType {
		// book keeping
		Unknown,
		Invalid,
		// 3420 types
		Geiger,
		Train,
		HudText,
		SayText,
		SayText2,
		TextMsg,
		HUDMsg,
		ResetHUD,   // called every respawn
		GameTitle,  // show game title
		ItemPickup, // for item history on screen
		ShowMenu,
		Shake,
		Fade,
		VGUIMenu,
		Rumble,     // Send a rumble to a controller
		Battery,
		Damage,     // for HUD damage indicators
		VoiceMask,
		RequestState,
		CloseCaption,
		HintText,
		KeyHintText,
		SquadMemberDied,
		AmmoDenied,
		CreditsMsg,
		CreditsPortalMsg,
		LogoTimeMsg,
		AchievementEvent,
		EntityPortalled,
		KillCam,
		// additional types from unpack
		SPHapWeapEvent,
		HapDmg,
		HapPunch,
		HapSetDrag,
		HapSetConst,
		HapMeleeContact,
		// additional types from portal 2
		Tilt,
		CloseCaptionDirect,
		UpdateJalopyRadar,
		CurrentTimescale,
		DesiredTimescale,
		InventoryFlash,
		IndicatorFlash,
		ControlHelperAnimate,
		TakePhoto,
		Flash,
		HudPingIndicator,
		OpenRadialMenu,
		AddLocator,
		MPMapCompleted,
		MPMapIncomplete,
		MPMapCompletedData,
		MPTauntEarned,
		MPTauntUnlocked,
		MPTauntLocked,
		MPAllTauntsLocked,
		PortalFX_Surface,
		PaintWorld,
		PaintEntity,
		ChangePaintColor,
		PaintBombExplode,
		RemoveAllPaint,
		PaintAllSurfaces,
		RemovePaint,
		StartSurvey,
		ApplyHitBoxDamageEffect,
		SetMixLayerTriggerFactor,
		TransitionFade,
		ScoreboardTempUpdate,
		ChallengeModCheatSession,
		ChallengeModCloseAllUI,
		// additional types from portal 1 steampipe
		CallVoteFailed,
		VoteStart,
		VotePass,
		VoteFailed,
		VoteSetup,
		// additional types from l4d2
		SendAudio,
		RawAudio,
		BarTime,
		RadioText,
		ReloadEffect,
		PlayerAnimEvent,
		UpdateRadar,
		MarkAchievement,
		Splatter,
		MeleeSlashSplatter,
		MeleeClubSplatter,
		MudSplatter,
		SplatterClear,
		MessageText,
		TransitionRestore,
		Spawn,
		CreditsLine,
		JoinLateMsg,
		StatsCrawlMsg,
		StatsSkipState,
		ShowStats,
		BlurFade,
		MusicCmd,
		WitchBloodSplatter,
		PZDmgMsg,
		AllPlayersConnectedGameStarting,
		VoteRegistered,
		DisconnectToLobby,
		SteamWeaponStatData,
		PZEndGamePanelMsg,
		PZEndGamePanelVoteRegisteredMsg,
		PZEndGameVoteStatsMsg,
		// additional types from l4d1
		HideLoadingPlaque,
	}
}
