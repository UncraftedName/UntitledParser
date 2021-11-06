using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DemoParser.Parser.Components;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.HelperClasses;
using DemoParser.Parser.HelperClasses.EntityStuff;
using DemoParser.Utils;
using static DemoParser.Parser.SourceGame;

namespace DemoParser.Parser {
	
	/// <summary>
	/// A class containing many useful constants used while parsing the demo.
	/// </summary>
	public class DemoInfo {
		
		// these seem to be constant in all games
		public const int MaxEdictBits = 11;
		public const int MaxEdicts = 1 << MaxEdictBits;
		public const int NetworkedEHandleSerialNumBits = 10;
		public const int NetworkedEHandleBits = MaxEdictBits + NetworkedEHandleSerialNumBits;
		public const uint NullEHandle = (1 << NetworkedEHandleBits) - 1; // INVALID_NETWORKED_EHANDLE_VALUE
		public const int SubStringBits = 5;
		public const int MaxUserDataBits = 14;
		public const uint HandleSerialNumberBits = 10;
		public const int MaxPortal2CoopBranches = 6;
		public const int MaxPortal2CoopLevelsPerBranch = 16;
		public const int MaxNetMessage = 6;
		public const int AreaBitsNumBits = 8;
		
		public const int MaxSndIndexBits = 13;
		public const int SndSeqNumberBits = 10;
		public const int MaxSndLvlBits = 9;
		public const int MaxSndDelayMSecEncodeBits = 13;
		public const float SndDelayOffset = 0.1f;
		public const int SndSeqNumMask = (1 << SndSeqNumberBits) - 1;

		public const int MaxPlayerNameLength = 32;
		public const int SignedGuidLen = 32;
		

		// initialized from the header
		public readonly SourceGame Game;
		public readonly int MaxSplitscreenPlayers;
		public readonly int SignOnGarbageBytes;
		public readonly bool ProcessEnts; // only do ent stuff if you're testing or the game is supported
		public readonly IReadOnlyList<TimingAdjustment.AdjustmentType> TimeAdjustmentTypes;
		public readonly int SendPropFlagBits;
		public readonly int SoundFlagBits;
		public readonly bool NewDemoProtocol; // "new engine" in nekz' parser
		public readonly int NetMsgTypeBits;
		public readonly int UserMessageLengthBits;
		public readonly int SendPropNumBitsToGetNumBits;
		public readonly int NumNetFileFlagBits;
		public float TickInterval; // I set the tick interval here just in case but it should get set from SvcServerInfo
		public bool HasParsedTickInterval = false;
		
		
		// game specific enum lists
		public readonly AbstractFlagChecker<SendPropEnums.PropFlag> PropFlagChecker;
		public readonly AbstractFlagChecker<PropEnums.PlayerMfFlags_t> PlayerMfFlagChecker;
		public readonly IReadOnlyList<PropEnums.Collision_Group_t> CollisionsGroupList;
		
		public readonly IReadOnlyList<SendPropEnums.SendPropType> SendPropTypes;
		public readonly IReadOnlyDictionary<SendPropEnums.SendPropType, int> SendPropTypesReverseLookup;
		public readonly IReadOnlyList<PacketType>? PacketTypes;
		public readonly IReadOnlyDictionary<PacketType, int>? PacketTypesReverseLookup;
		public readonly IReadOnlyList<MessageType>? MessageTypes;
		public readonly IReadOnlyDictionary<MessageType, int>? MessageTypesReverseLookup;
		public readonly IReadOnlyList<UserMessageType>? UserMessageTypes;
		public readonly IReadOnlyDictionary<UserMessageType, int>? UserMessageTypesReverseLookup;
		
		
		public DemoInfo(DemoHeader h) {
			switch (h.DemoProtocol) {
				case 3:
					switch (h.NetworkProtocol) {
						case 7:
							Game = HL2_OE;
							PacketTypes = DemoPacket.Portal3420Table;
							UserMessageTypes = UserMessage.Hl2OeTable;
							ProcessEnts = true;
							MaxSplitscreenPlayers = 1;
							SignOnGarbageBytes = 76;
							TickInterval = 1f / 66;
							break;
						case 14:
							Game = PORTAL_1_3420;
							PacketTypes = DemoPacket.Portal3420Table;
							UserMessageTypes = UserMessage.Portal3420Table;
							ProcessEnts = true;
							MaxSplitscreenPlayers = 1;
							SignOnGarbageBytes = 76;
							TickInterval = 1f / 66;
							break;
						case 15:
							Game = PORTAL_1_5135;
							PacketTypes = DemoPacket.Portal1UnpackTable;
							UserMessageTypes = UserMessage.Portal1UnpackTable;
							ProcessEnts = true;
							MaxSplitscreenPlayers = 1;
							SignOnGarbageBytes = 76;
							TickInterval = 1f / 66;
							break;
						case 24:
							Game = PORTAL_1_STEAMPIPE;
							PacketTypes = DemoPacket.Portal1UnpackTable;
							UserMessageTypes = UserMessage.Portal1SteamTable;
							MaxSplitscreenPlayers = 1;
							SignOnGarbageBytes = 76;
							TickInterval = 1f / 66;
							break;
						default:
							Game = UNKNOWN;
							// just guess, (copy of steampipe)
							PacketTypes = DemoPacket.Portal1UnpackTable;
							UserMessageTypes = UserMessage.Portal1SteamTable;
							MaxSplitscreenPlayers = 1;
							SignOnGarbageBytes = 76;
							break;
					}
					break;
				case 4:
					switch (h.NetworkProtocol) {
						case 37:
							Game = L4D1_1005;
							PacketTypes = DemoPacket.DemoProtocol4Table;
							UserMessageTypes = UserMessage.L4D1OldTable;
							MaxSplitscreenPlayers = 4;
							SignOnGarbageBytes = 304;
							TickInterval = 1f / 30;
							break;
						case 2000:
							Game = L4D2_2000;
							PacketTypes = DemoPacket.DemoProtocol4Table;
							UserMessageTypes = UserMessage.L4D2SteamTable;
							MaxSplitscreenPlayers = 4;
							SignOnGarbageBytes = 304;
							TickInterval = 1f / 30;
							break;
						case 2001:
							Game = PORTAL_2;
							PacketTypes = DemoPacket.DemoProtocol4Table;
							UserMessageTypes = UserMessage.Portal2Table;
							ProcessEnts = true;
							MaxSplitscreenPlayers = 2;
							SignOnGarbageBytes = 152;
							TickInterval = 1f / 60;
							break;
						case 2042:
							Game = L4D2_2042;
							PacketTypes = DemoPacket.DemoProtocol4Table;
							UserMessageTypes = UserMessage.L4D2SteamTable;
							MaxSplitscreenPlayers = 4;
							SignOnGarbageBytes = 304;
							TickInterval = 1f / 30;
							break;
						default:
							Game = UNKNOWN;
							// just guess, (copy of p2)
							PacketTypes = DemoPacket.DemoProtocol4Table;
							UserMessageTypes = UserMessage.Portal2Table;
							ProcessEnts = true;
							MaxSplitscreenPlayers = 2;
							SignOnGarbageBytes = 152;
							TickInterval = 1f / 60;
							break;
					}
					break;
				default:
					Game = UNKNOWN;
					break;
			}
			UserMessageTypesReverseLookup = UserMessageTypes?.CreateReverseLookupDict();
			PacketTypesReverseLookup = PacketTypes?.CreateReverseLookupDict(PacketType.Invalid);
			if (Game == UNKNOWN) {
				ParserTextUtils.ConsoleWriteWithColor(
					$"\nUnknown game, demo might not parse correctly. Update in {GetType().FullName}.\n",
					ConsoleColor.Magenta);
			}

			// these should all come last after the game and other constants have already been determined

			if (h.NetworkProtocol <= 14) {
				NetMsgTypeBits = 5;
				SendPropNumBitsToGetNumBits = 6;
				SendPropTypes = SendPropEnums.OldNetPropTypes;
			} else {
				SendPropNumBitsToGetNumBits = Game == L4D1_1005 || Game == L4D2_2000 || Game == L4D2_2042 ? 6 : 7;
				NetMsgTypeBits = 6;
				SendPropTypes = SendPropEnums.NewNetPropTypes;
			}
			SendPropTypesReverseLookup = SendPropTypes.CreateReverseLookupDict();
			
			switch (h.DemoProtocol) {
				case 2:
					NewDemoProtocol = false;
					SendPropFlagBits = 11;
					goto default; // I don't know any other constants for demo protocol 2 so just cry
				case 3:
				case 4 when Game == L4D1_1005:
					NewDemoProtocol = false;
					SendPropFlagBits = 16;
					SoundFlagBits = 9;
					PropFlagChecker = new SendPropEnums.DemoProtocol3FlagChecker();
					PlayerMfFlagChecker = new PropEnums.PlayerMfFlagsOldDemoProtocol();
					CollisionsGroupList = PropEnums.CollisionGroupListOldDemoProtocol;
					UserMessageLengthBits = 11;
					NumNetFileFlagBits = 1;
					if (h.NetworkProtocol <= 7)
						MessageTypes = DemoMessage.OeMessageList;
					else if (h.NetworkProtocol < 24)
						MessageTypes = DemoMessage.OldProtocolMessageList;
					else
						MessageTypes = DemoMessage.SteamPipeMessageList;
					break;
				case 4:
					NewDemoProtocol = true;
					SendPropFlagBits = 19;
					SoundFlagBits = 13;
					//SoundFlagBits = 9;
					UserMessageLengthBits = Game == L4D2_2042 ? 11 : 12;
					PropFlagChecker = new SendPropEnums.DemoProtocol4FlagChecker();
					if (Game == PORTAL_2) {
						PlayerMfFlagChecker = new PropEnums.PlayerMfFlagsPortal2();
						CollisionsGroupList = PropEnums.CollisionGroupListPortal2;
					} else {
						PlayerMfFlagChecker = new PropEnums.PlayerMfFlagsNewDemoProtocol();
						CollisionsGroupList = PropEnums.CollisionGroupListNewDemoProcol;
					}
					NumNetFileFlagBits = 2;
					MessageTypes = DemoMessage.NewProtocolMessageList;
					break;
				default:
					throw new ArgumentException($"What the heck is demo protocol version {h.DemoProtocol}?");
			}
			MessageTypesReverseLookup = MessageTypes.CreateReverseLookupDict(MessageType.Invalid);
			
			TimeAdjustmentTypes = TimingAdjustment.AdjustmentTypeFromMap(h.MapName, Game);
		}
	}
	
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	public enum SourceGame {
		HL2_OE,
		PORTAL_1_3420,
		PORTAL_1_5135, // todo add hl2
		PORTAL_1_STEAMPIPE,

		PORTAL_2,

		L4D1_1005,
		L4D2_2000,
		L4D2_2042,
		UNKNOWN
	}
}
