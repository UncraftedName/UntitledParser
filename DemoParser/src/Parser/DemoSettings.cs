using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DemoParser.Parser.Components;
using DemoParser.Parser.HelperClasses;
using DemoParser.Parser.HelperClasses.EntityStuff;
using DemoParser.Utils;
using static DemoParser.Parser.SourceGame;

namespace DemoParser.Parser {
	
	/// <summary>
	/// A class containing many useful constants used while parsing the demo.
	/// </summary>
	public class DemoSettings {
		// these seem to be constant in all games
		public const int MaxEdictBits = 11;
		public const int MaxEdicts = 1 << MaxEdictBits;
		public const int SubStringBits = 5;
		public const int NumNetworkedEHandleBits = 10;
		public const int MaxUserDataBits = 14;
		public const uint HandleSerialNumberBits = 10;
		public const int MaxPortal2CoopBranches = 6;
		public const int MaxPortal2CoopLevelsPerBranch = 16;
		
		public const int MaxSndIndexBits = 13;
		public const int SndSeqNumberBits = 10;
		public const int MaxSndLvlBits = 9;
		public const int MaxSndDelayMSecEncodeBits = 13;
		public const float SndDelayOffset = 0.1f;
		public const int SndSeqNumMask = (1 << SndSeqNumberBits) - 1;
		

		// initialized from the header
		public readonly SourceGame Game;
		public readonly int MaxSplitscreenPlayers;
		public readonly int SignOnGarbageBytes;
		public readonly int SvcServerInfoUnknownBits;
		public float TickInterval; // to be determined while parsing in SvcServerInfo
		public readonly bool ProcessEnts; // only do ent stuff if you're testing or the game is supported
		public readonly IReadOnlyList<TimingAdjustment.AdjustmentType> TimeAdjustmentTypes;
		public readonly int SendPropFlagBits;
		public readonly int SoundFlagBits;
		public readonly bool NewDemoProtocol; // "new engine" in nekz' parser
		public readonly int NetMsgTypeBits;
		public readonly int UserMessageLengthBits;
		
		// game specific enum lists
		public readonly AbstractFlagChecker<PropFlag> PropFlagChecker;
		public readonly PropEnums.Collision_Group_t[] CollisionsGroupList;
		public readonly AbstractFlagChecker<PropEnums.PlayerMfFlags_t> PlayerMfFlagChecker;
		// todo add user message/packet list
		
		
		public DemoSettings(DemoHeader h) {
			switch (h.DemoProtocol) {
				case 3 when h.NetworkProtocol == 14:
					Game = PORTAL_1_3420;
					break;
				case 3 when h.NetworkProtocol == 15:
					Game = PORTAL_1_UNPACK;
					break;
				case 3 when h.NetworkProtocol == 24:
					Game = PORTAL_1_STEAMPIPE;
					break;
				case 4 when h.NetworkProtocol == 2001:
					Game = PORTAL_2;
					break;
				case 4 when h.NetworkProtocol == 2000:
					Game = L4D2_2000;
					break;
				case 4 when h.NetworkProtocol == 2042:
					Game = L4D2_2042;
					break;
				default:
					Game = UNKNOWN;
					Console.WriteLine($"\nUnknown game, demo might not parse correctly. Update in {GetType().FullName}.\n");
					break;
			}
			
			if (Game == L4D2_2042)
				SvcServerInfoUnknownBits = 33;
			else if (NewDemoProtocol)
				SvcServerInfoUnknownBits = 32;
			else if (h.NetworkProtocol == 24)
				SvcServerInfoUnknownBits = 96;
			else
				SvcServerInfoUnknownBits = 0;
			
			switch (Game) {
				case PORTAL_1_UNPACK:
				case PORTAL_1_3420:
					ProcessEnts = true;
					goto case PORTAL_1_STEAMPIPE;
				case PORTAL_1_STEAMPIPE:
					MaxSplitscreenPlayers = 1;
					SignOnGarbageBytes = 76;
					break;
				case PORTAL_2:
					ProcessEnts = true;
					MaxSplitscreenPlayers = 2;
					SignOnGarbageBytes = 152;
					break;
				case L4D2_2000:
				case L4D2_2042:
					MaxSplitscreenPlayers = 4;
					SignOnGarbageBytes = 304;
					break;
				case UNKNOWN:
					MaxSplitscreenPlayers = 4;
					SignOnGarbageBytes = 152;
					break;
				default:
					throw new Exception("You fool, you absolute buffoon! " +
										"You added a game type but didn't add a case for it!");
			}
			NetMsgTypeBits = h.NetworkProtocol == 14 ? 5 : 6;
			// these should all come last after the game and other constants have already been determined
			switch (h.DemoProtocol) {
				case 2:
					NewDemoProtocol = false;
					SendPropFlagBits = 11;
					goto default; // I don't know any other constants for demo protocol 2 so just cry
				case 3:
					NewDemoProtocol = false;
					SendPropFlagBits = 16;
					SoundFlagBits = 9;
					PropFlagChecker = new DemoProtocol3FlagChecker();
					PlayerMfFlagChecker = new PropEnums.PlayerMfFlagsOldDemoProtocol();
					CollisionsGroupList = PropEnums.CollisionGroupListOldDemoProtocol;
					UserMessageLengthBits = 11;
					break;
				case 4:
					NewDemoProtocol = true;
					SendPropFlagBits = 19;
					SoundFlagBits = 13;
					PropFlagChecker = new DemoProtocol4FlagChecker();
					UserMessageLengthBits = Game == L4D2_2042 ? 11 : 12;
					if (Game == PORTAL_2) {
						PlayerMfFlagChecker = new PropEnums.PlayerMfFlagsPortal2();
						CollisionsGroupList = PropEnums.CollisionGroupListPortal2;
					} else {
						PlayerMfFlagChecker = new PropEnums.PlayerMfFlagsNewDemoProtocol();
						CollisionsGroupList = PropEnums.CollisionGroupListNewDemoProcol;
					}
					break;
				default:
					throw new ArgumentException($"What the heck is demo protocol version {h.DemoProtocol}?");
			}
			
			TimeAdjustmentTypes = TimingAdjustment.AdjustmentTypeFromMap(h.MapName, Game);
			
#if FORCE_PROCESS_ENTS
			ProcessEnts = true; // be prepared for lots of exceptions
#endif
		}
	}
	
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	public enum SourceGame {
		PORTAL_1_UNPACK, // todo add hl2
		PORTAL_1_3420,
		PORTAL_1_STEAMPIPE,
		PORTAL_2,
		L4D2_2000,
		L4D2_2042,
		UNKNOWN
	}
}
