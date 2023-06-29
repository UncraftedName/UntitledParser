using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using DemoParser.Parser.Components.Messages;
using DemoParser.Parser.Components.Messages.UserMessages;
using DemoParser.Parser.Components.Packets;
using DemoParser.Parser.EntityStuff;
using DemoParser.Utils;
using static DemoParser.Parser.DemoInfo;
using static DemoParser.Parser.EntityStuff.PropEnums.PlayerMfFlags_t;
using static DemoParser.Parser.TimingAdjustment.AdjustmentType;
using static DemoParser.Parser.SourceGame;
using DemoParser.Parser.Components.Abstract;

namespace DemoParser.Parser {


	// credit to iVerb for the portal2 commands https://github.com/iVerb1/SourceLiveTimer/tree/master/Demo/GameHandler
	// The portal 2 / steampipe ones need to be redone once I figure out ent info. I only add explicit support
	// for unpack atm but hopefully that will change in the future.
	[SuppressMessage("ReSharper", "StringLiteralTypo")]
	[SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
	public static class TimingAdjustment {

		public static readonly ICollection<(SourceGame game, string mapName)> ExcludedMaps = new [] {
			(PORTAL_1_5135, "gex03"),      // gamma energy last map
			(PORTAL_1_5135, "rex_menu"),   // rexaura menu (why tf does this even have a demo???)
			(PORTAL_1_1910503, "rex_menu"),
			(PORTAL_1_3420, "ricky_credits"),
			(PORTAL_1_5135, "ricky_credits"),
			(PORTAL_1_1910503, "ricky_credits"),
		}.ToImmutableHashSet();


		// In vanilla you can check for game begin/end without doing ent parsing, so I use that so that the this works
		// with steampipe.
		public static IReadOnlyList<AdjustmentType> AdjustmentTypeFromMap(string mapName, SourceGame game, SequenceType sequenceIndicator) {
			switch (game) {
				case PORTAL_1_5135:
					switch (mapName) {
						case "portaltfv5":
						case "stillalive_14_withending2":
							return new[] {TfvEnd};
						case "start":        // portal pro
						case "testchmb00":   // portal unity
						case "rex_00":       // rexaura
							return new[] {Portal1GenericWakeup};
						case "boss":
							return new[] {PortalProEnd};
						case "escape03":
							return new[] {PortalUnityEnd};
						case "rex_19":
							return new[] {RexauraEnd};
						case "level_01":
							return new[] {PortalPreludeBegin};
						case "level_08":
							return new[] {PortalPreludeEnd};
					}
					goto case PORTAL_1_3420;

				case PORTAL_1_1910503: // todo once ents work for steampipe, add above mods (and check if map names are the same)
				case PORTAL_1_3420:
					switch (mapName) {
						case "testchmb_a_00":
							return new[] {Portal1Begin};
						case "escape_02":
						case "escape_02_d_180": // pcborrr
							return new[] {Portal1End};
					}
					break;

				case PORTAL_2:
					switch (mapName) {
						case "sp_a1_intro1":
							return new[] {Portal2Begin};
						case "sp_a4_finale4":
							return new[] {Portal2End};
						case "mp_coop_start":
							return new[] {Portal2CoopBegin, Portal2CoopMapEnd};
						case "mp_coop_paint_longjump_intro":
							return new[] {Portal2CoopMapStart, Portal2CoopEnd};
						case "mp_coop_paint_crazy_box":
							return new[] {Portal2CoopMapStart, Portal2CoopEndCourse6};
						case "mp_coop_lobby_3":
						case "mp_coop_doors": // course 1
						case "mp_coop_race_2":
						case "mp_coop_laser_2":
						case "mp_coop_rat_maze":
						case "mp_coop_laser_crusher":
						case "mp_coop_teambts":
						case "mp_coop_fling_3": // course 2
						case "mp_coop_infinifling_train":
						case "mp_coop_come_along":
						case "mp_coop_fling_1":
						case "mp_coop_catapult_1":
						case "mp_coop_multifling_1":
						case "mp_coop_fling_crushers":
						case "mp_coop_fan":
						case "mp_coop_wall_intro": // course 3
						case "mp_coop_wall_2":
						case "mp_coop_catapult_wall_intro":
						case "mp_coop_wall_block":
						case "mp_coop_catapult_2":
						case "mp_coop_turret_walls":
						case "mp_coop_turret_ball":
						case "mp_coop_wall_5":
						case "mp_coop_tbeam_redirect": // course 4
						case "mp_coop_tbeam_drill":
						case "mp_coop_tbeam_catch_grind_1":
						case "mp_coop_tbeam_laser_1":
						case "mp_coop_tbeam_polarity":
						case "mp_coop_tbeam_polarity2":
						case "mp_coop_tbeam_polarity3":
						case "mp_coop_tbeam_maze":
						case "mp_coop_tbeam_end":
						case "mp_coop_paint_come_along": // course 5
						case "mp_coop_paint_redirect":
						case "mp_coop_paint_bridge":
						case "mp_coop_paint_walljumps":
						case "mp_coop_paint_speed_fling":
						case "mp_coop_paint_red_racer":
						case "mp_coop_paint_speed_catch":
						case "mp_coop_separation_1": // course 6
						case "mp_coop_tripleaxis":
						case "mp_coop_catapult_catch":
						case "mp_coop_2paints_1bridge":
						case "mp_coop_paint_conversion":
						case "mp_coop_bridge_catch":
						case "mp_coop_laser_tbeam":
						case "mp_coop_paint_rat_maze":
							return new[] {Portal2CoopMapStart, Portal2CoopMapEnd};
					}
					break;
				case L4D1_1005:
				case L4D1_1040:
				case L4D2_2000:
				case L4D2_2012:
				case L4D2_2027:
				case L4D2_2042:
				case L4D2_2091:
				case L4D2_2147:
				case L4D2_2203:
					switch (mapName.ToLower()) {
						case "l4d_hospital01_apartment":
						case "l4d_smalltown01_caves":
						case "l4d_airport01_greenhouse":
						case "l4d_farm01_hilltop":
						case "l4d_river01_docks":
						case "l4d_garage01_alleys":
						case "c1m1_hotel":
						case "c2m1_highway":
						case "c3m1_plankcountry":
						case "c4m1_milltown_a":
						case "c5m1_waterfront":
						case "c6m1_riverbank":
						case "c7m1_docks":
						case "c8m1_apartment":
						case "c9m1_alleys":
						case "c10m1_caves":
						case "c11m1_greenhouse":
						case "c12m1_hilltop":
						case "c13m1_alpinecreek":
						case "c14m1_junkyard":
							if (sequenceIndicator == SequenceType.FirstDemo || sequenceIndicator == SequenceType.SingleDemo)
								return new[] {L4DBegin};
							else
								return new[] {L4DCampaignStart};
						case "l4d_hospital05_rooftop":
						case "l4d_smalltown05_houseboat":
						case "l4d_airport05_runway":
						case "l4d_farm05_cornfield":
						case "l4d_river03_port":
						case "l4d_garage02_lots":
						case "c1m4_atrium":
						case "c2m5_concert":
						case "c3m4_plantation":
						case "c4m5_milltown_escape":
						case "c5m5_bridge":
						case "c6m3_port":
						case "c7m3_port":
						case "c8m5_rooftop":
						case "c9m2_lots":
						case "c10m5_houseboat":
						case "c11m5_runway":
						case "c12m5_cornfield":
						case "c13m4_cutthroatcreek":
						case "c14m2_lighthouse":
							if (sequenceIndicator == SequenceType.LastDemo || sequenceIndicator == SequenceType.SingleDemo)
								return new[] {L4DMapStart, L4DEnd};
							goto default;
						default:
							return new[] {L4DMapStart};
					}
			}
			return new AdjustmentType[] {};
		}


		public static void AdjustFromConsoleCmd(ConsoleCmd consoleCmd) {
			if (consoleCmd.Tick <= 0)
				return;

			ref int? start = ref consoleCmd.DemoRef.StartAdjustmentTick;
			ref int? end = ref consoleCmd.DemoRef.EndAdjustmentTick;

			// assume only one valid adjustment per cmd
			foreach (AdjustmentType type in consoleCmd.DemoRef.DemoInfo.TimeAdjustmentTypes) {
				switch (type) {
					case Portal2CoopMapStart when !start.HasValue && consoleCmd.Command == "ss_force_primary_fullscreen 0":
						start = consoleCmd.Tick;
						return;
					case Portal2CoopMapEnd when consoleCmd.Command.StartsWith("playvideo_end_level_transition"):
					case Portal2CoopEnd when consoleCmd.Command == "playvideo_exitcommand_nointerrupt coop_outro end_movie vault-movie_outro":
					case Portal2CoopEndCourse6 when consoleCmd.Command == "playvideo_exitcommand_nointerrupt dlc1_endmovie end_movie movie_outro":
						end = consoleCmd.Tick;
						return;
					case Portal1End when consoleCmd.Command == "startneurotoxins 99999":
						end = consoleCmd.Tick + 1; // not sure why add 1
						return;
				}
			}
		}

		public static void AdjustFromUserCmd(UserCmd userCmd) {
			ref int? start = ref userCmd.DemoRef.StartAdjustmentTick;
			//ref int? end = ref userCmd.DemoRef.EndAdjustmentTick;

			foreach (AdjustmentType type in userCmd.DemoRef.DemoInfo.TimeAdjustmentTypes) {
				switch (type) {
					// fallback in case there's no intro cutscene
					case L4DBegin when !start.HasValue:
					case L4DCampaignStart when !start.HasValue:
					// first UserCmd in general
					case L4DMapStart when !start.HasValue:
					// first UserCmd after some specific Packet
					// this is an attempt to stay close to video timing without losing timing fairness
					// (this is equally unfair compared to choosing the aforementioned Packet as the start tick)
					case L4DCampaignStart when start < 0:
						start = userCmd.Tick;
						return;
				}
			}
		}


		public static void AdjustFromPacket(Packet packet) {
			if (packet.Tick <= 0)
				return;

			ref int? start = ref packet.DemoRef.StartAdjustmentTick;
			ref int? end = ref packet.DemoRef.EndAdjustmentTick;

			// assume only one valid adjustment per packet
			foreach (AdjustmentType type in packet.DemoRef.DemoInfo.TimeAdjustmentTypes) {
				switch (type) {
					case L4DCampaignStart when L4DCampaignPosterHideCheck(packet):
						start = -packet.Tick; // negative value as signal for AdjustFromUserCmd
						return;
					case L4DBegin when L4DControlGainCheck(packet):
					case Portal2CoopBegin when !start.HasValue && Portal2CoopStartCheck(packet):
						start = packet.Tick;
						return;
					case Portal1GenericWakeup when Portal1WakeupCheck(packet):
					case Portal1Begin when Portal1VanillaWakeupCheck(packet):
					case PortalPreludeBegin when PreludeWakeupCheck(packet):
					case Portal2Begin when Portal2AtSpawn(packet):
						start = packet.Tick + 1;
						return;
					case TfvEnd when TfvCheckGunShipSpawn(packet):
						end = packet.Tick - 1; // gun ship spawns a tick after trigger
						return;
					case PortalUnityEnd when PortalUnityEndCheck(packet):
					case PortalProEnd when PortalProEndCheck(packet):
					case RexauraEnd when RexauraEndCheck(packet):
					case PortalPreludeEnd when PreludeEndCheck(packet):
						end = packet.Tick + 1;
						return;
					case Portal2End when Portal2PortalOnMoon(packet):
					case L4DEnd when L4DEscapeTriggeredCheck(packet):
						end = packet.Tick;
						return;
				}
			}
		}


		// using net cmd's is probably a bit more tamper safe, but this could easily be set to search in ConsoleCmd
		private static bool PreludeEndCheck(Packet packet) {
			var commands = packet.FilterForMessage<NetStringCmd>().Select(cmd => cmd.Command).ToList();
			return commands.Any(s => s.StartsWith("crosshair 0")) &&
				   commands.Any(s => s.StartsWith("startneurotoxins 120"));
		}


		// prelude kinda dummy - game sets your pos 3 times during wakeup so I check local view angles too
		private static bool PreludeWakeupCheck(Packet packet) {
			return packet.PacketInfo[0].LocalViewAngles.Y == 180f &&
				   packet.FilterForMessage<SvcFixAngle>()
					   .Any(svcFixAngle => svcFixAngle.Angle == new Vector3(0f, 190.003052f, 0f));
		}


		private static bool RexauraEndCheck(Packet packet) { // yeah idk
			return packet.FilterForUserMessage<KillCam>().Any(cam => (byte)cam.SpecMode == 172) && Portal1GunRemoved(packet);
		}


		private static bool Portal2CoopStartCheck(Packet packet) {
			return packet.PacketInfo[0].ViewOrigin == new Vector3(-9896f, -4400f, 3048f) ||
				   packet.PacketInfo[0].ViewOrigin == new Vector3(-11168f, -4384f, 3040.03125f);
		}


		private static bool Portal1VanillaWakeupCheck(Packet packet) {
			return packet.FilterForMessage<SvcFixAngle>()
				.Any(svcFixAngle => svcFixAngle.Angle == new Vector3(0f, 189.997559f, 0f));
		}


		private static bool Portal2AtSpawn(Packet packet) {
			return packet.FilterForMessage<SvcFixAngle>().Any()
				   && packet.FilterForMessage<SvcPacketEntities>().Any(
					   entityMsg => entityMsg.Updates!
						   .OfType<Delta>()
						   .First(delta => delta.EntIndex == 1).Props
						   .Select(tuple => tuple.prop)
						   .OfType<SingleEntProp<int>>()
						   .Any(prop => prop == NullEHandle && prop.Name == "m_hViewEntity"));
		}


		private static bool Portal2PortalOnMoon(Packet packet) {
			var portalVecDeltas = packet.FilterForMessage<SvcPacketEntities>()
				.SelectMany(entityMsg => entityMsg.Updates!)
				.OfType<Delta>()
				.Where(delta => delta.ServerClass.ClassName == "CProp_Portal")
				.SelectMany(delta => delta.Props)
				.Select(tuple => tuple.prop)
				.OfType<SingleEntProp<Vector3>>()
				.ToList();

			bool originCheck = false, rotationCheck = false;

			foreach (SingleEntProp<Vector3> prop in portalVecDeltas) {
				ref Vector3 val = ref prop.Value;
				if (prop.Name == "m_angRotation")
					rotationCheck = val.X == 90 && val.Z == 0;
				else if (prop.Name == "m_ptOrigin")
					originCheck =
						185 < val.X && val.X < 485 &&
						-95 < val.Y && val.Y < 195 &&
						Math.Abs(val.Z - 1543.968) < 0.001;
			}
			return originCheck && rotationCheck;
		}


		private static bool TfvCheckGunShipSpawn(Packet packet) {
			return packet.FilterForMessage<SvcPacketEntities>()
				.Any(entityMessage => entityMessage.Updates!
					.OfType<EnterPvs>()
					.Any(enterMsg => enterMsg.New && enterMsg.ServerClass.ClassName == "CNPC_CombineGunship"));
			/*return packet.FilterForMessage<SvcPacketEntities>()
				.Any(entityMessage => entityMessage.Updates.OfType<Delta>()
					.First(delta => delta.EntIndex == 1).Props
					.Select(tuple => tuple.prop)
					.OfType<FloatEntProp>()
					.Any(prop => prop.Value == 0.400000006f && prop.Name == "localdata.m_flLaggedMovementValue"));*/
		}


		private static bool Portal1WakeupCheck(Packet packet) {
			return packet.FilterForMessage<SvcSetView>().Any(viewMsg => viewMsg.EntityIndex == 1) &&
				   packet.FilterForMessage<SvcPacketEntities>()
					   .Any(entityMessage => entityMessage.Updates!
						   .OfType<Delta>()
						   .First(delta => delta.EntIndex == 1).Props
						   .Select(tuple => tuple.prop)
						   .OfType<SingleEntProp<int>>()
						   .Any(prop => prop.Name == "m_fFlags" && packet.DemoRef.DemoInfo.PlayerMfFlagChecker.HasFlags(
							   prop, FL_ONGROUND, FL_CLIENT)));
		}


		private static bool PortalUnityEndCheck(Packet packet) {
			return packet.FilterForUserMessage<Fade>().Any(fade => fade.Flags == (FadeFlags.FadeIn | FadeFlags.Purge)) &&
				   Portal1GunRemoved(packet) &&
				   packet.FilterForMessage<SvcPacketEntities>()
					   .Single().Updates!
					   .OfType<Delta>()
					   .First(delta => delta.EntIndex == 1).Props
					   .Select(tuple => tuple.prop)
					   .OfType<SingleEntProp<int>>()
					   .Any(prop => prop.Name == "m_fFlags" && packet.DemoRef.DemoInfo.PlayerMfFlagChecker.HasFlags(
						   prop, FL_ONGROUND, FL_FROZEN, FL_CLIENT));
		}


		private static bool PortalProEndCheck(Packet packet) {
			return packet.FilterForUserMessage<Fade>()
					   .Any(fade => fade.Flags == (FadeFlags.FadeOut | FadeFlags.Purge)) && Portal1GunRemoved(packet);
		}


		private static bool Portal1GunRemoved(Packet packet) {
			return packet.FilterForMessage<SvcPacketEntities>()
				.Single().Updates!
				.OfType<Delta>()
				.First(delta => delta.EntIndex == 1).Props
				.Select(tuple => tuple.prop)
				.OfType<SingleEntProp<int>>()
				.Any(prop => prop == NullEHandle && prop.Name == "m_hActiveWeapon");
		}


		private static bool L4DControlGainCheck(Packet packet) {
			// other potential props to check are moveparent and m_hViewEntity
			// but not on the exact same tick as they aren't always updated on the same tick
			return packet.FilterForMessage<SvcPacketEntities>()
				.Any(entityMessage => entityMessage.Updates!.OfType<Delta>()
					.Where(delta => delta.EntIndex == 1)
					.SelectMany(delta => delta.Props)
					.Select(tuple => tuple.prop)
					.OfType<SingleEntProp<int>>()
					.Any(prop => prop == NullEHandle && prop.Name == "m_positionEntity"));
		}


		private static bool L4DCampaignPosterHideCheck(Packet packet) {
			UserMessageType messageType = packet.DemoRef.DemoInfo.Game == L4D1_1005 ? UserMessageType.HideLoadingPlaque : UserMessageType.AllPlayersConnectedGameStarting;
			return packet.FilterForMessage<SvcUserMessage>()
				.Any(frame => frame.MessageType == messageType);
		}


		// Prioritize the finale_vehicle_leaving event and fallback on the last HideHUD + Flags combo
		// This logic isn't reliable on finales that have death/cutscene cameras and are breakable, but
		// those don't exist (yet)
		private static bool L4DFinaleVehicleLeft = false;
		private static bool L4DEscapeTriggeredCheck(Packet packet) {
			if (L4DFinaleVehicleLeft)
				return false;
			L4DFinaleVehicleLeft = packet.FilterForMessage<SvcGameEvent>()
				.Any(ev => ev.EventDescription.Name == "finale_vehicle_leaving");
			return L4DFinaleVehicleLeft || packet.FilterForMessage<SvcPacketEntities>()
				.Any(entityMessage => {
					var props = entityMessage.Updates!.OfType<Delta>()
						.Where(delta => delta.ServerClass.ClassName == "CTerrorPlayer" && delta.EntIndex == 1)
						.SelectMany(delta => delta.Props)
						.Select(tuple => tuple.prop)
						.OfType<SingleEntProp<int>>();
					// m_iHideHUD == HIDEHUD_WEAPONSELECTION, HIDEHUD_FLASHLIGHT, HIDEHUD_HEALTH, HIDEHUD_PLAYERDEAD, HIDEHUD_NEEDSUIT,
					// HIDEHUD_MISCSTATUS, HIDEHUD_CROSSHAIR, HIDEHUD_VEHICLE_CROSSHAIR, HIDEHUD_INVEHICLE, HIDEHUD_BONUS_PROGRESS
					return props.Any(prop => prop.Name == "m_Local.m_iHideHUD" && prop.Value == 3963) &&
						props.Any(prop => prop.Name == "m_fFlags" &&
							packet.DemoRef.DemoInfo.PlayerMfFlagChecker.HasFlags(prop, FL_FROZEN, FL_CLIENT));
				});
		}


		public enum AdjustmentType {
			Portal1Begin,
			Portal1End,

			Portal2Begin,
			Portal2End,

			Portal2CoopMapStart,
			Portal2CoopMapEnd,
			Portal2CoopBegin,
			Portal2CoopEnd,
			Portal2CoopEndCourse6,

			TfvEnd,
			RexauraEnd,
			Portal1GenericWakeup,
			PortalUnityEnd,
			PortalProEnd,
			PortalPreludeBegin,
			PortalPreludeEnd,

			L4DMapStart,
			L4DCampaignStart,
			L4DBegin,
			L4DEnd
		}

		public enum SequenceType {
			SingleDemo,
			FirstDemo,
			MidDemo,
			LastDemo
		}
	}
}
