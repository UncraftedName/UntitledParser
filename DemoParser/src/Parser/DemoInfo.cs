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


		// a way to tell what info did/didn't get parsed
		public DemoParseResult DemoParseResult;


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


		private static readonly IDictionary<(uint demoProtocol, uint networkProtocol), SourceGame> GameLookup
			= new Dictionary<(uint, uint), SourceGame>
			{
				[(3, 7)]    = HL2_OE,
				[(3, 14)]   = PORTAL_1_3420,
				[(3, 15)]   = PORTAL_1_5135,
				[(3, 24)]   = PORTAL_1_1910503,
				[(4, 37)]   = L4D1_1005,
				[(4, 1040)] = L4D1_1040,
				[(4, 2000)] = L4D2_2000,
				[(4, 2001)] = PORTAL_2,
				[(4, 2012)] = L4D2_2012,
				[(4, 2027)] = L4D2_2027,
				[(4, 2042)] = L4D2_2042,
				[(4, 2100)] = L4D2_2220,
			};


		public DemoInfo(SourceDemo demo) {
			DemoParseResult = default;
			DemoHeader h = demo.Header;

			if (!GameLookup.TryGetValue((h.DemoProtocol, h.NetworkProtocol), out Game)) {
				Game = UNKNOWN;
				demo.LogError($"\nUnknown game, demo might not parse correctly. Update in {GetType().FullName}.\n");
				DemoParseResult |= DemoParseResult.UnknownGame;
			}

			// provide default values in case of unknown game
			if (IsPortal1() || IsHL2() || (Game == UNKNOWN && h.DemoProtocol == 3)) {
				TickInterval = 0.015f;
				MaxSplitscreenPlayers = 1;
				if (h.NetworkProtocol <= 15)
					DemoParseResult |= DemoParseResult.EntParsingEnabled;
				PacketTypes = h.NetworkProtocol <= 14 ? DemoPacket.Portal3420Table : DemoPacket.Portal15135Table;
				if (h.NetworkProtocol <= 7)
					UserMessageTypes = UserMessage.Hl2OeTable;
				else if (h.NetworkProtocol <= 14)
					UserMessageTypes = UserMessage.Portal3420Table;
				else if (h.NetworkProtocol <= 15)
					UserMessageTypes = UserMessage.Portal5135Table;
				else
					UserMessageTypes = UserMessage.Portal1SteamTable;
			} else if (Game == PORTAL_2 || (Game == UNKNOWN && h.DemoProtocol == 4)) {
				TickInterval = 1f / 60;
				MaxSplitscreenPlayers = 2;
				if (Game == PORTAL_2)
					DemoParseResult |= DemoParseResult.EntParsingEnabled;
				PacketTypes = DemoPacket.DemoProtocol4Table;
				UserMessageTypes = UserMessage.Portal2Table;
			} else if (IsLeft4Dead()) {
				TickInterval = 1f / 30;
				MaxSplitscreenPlayers = 4;
				UserMessageTypes = IsLeft4Dead1() ? UserMessage.L4D1OldTable : UserMessage.L4D2SteamTable;
				PacketTypes = DemoPacket.DemoProtocol4Table;
			}

			UserMessageTypesReverseLookup = UserMessageTypes?.CreateReverseLookupDict();
			PacketTypesReverseLookup = PacketTypes?.CreateReverseLookupDict(PacketType.Invalid);

			if (h.NetworkProtocol <= 14) {
				NetMsgTypeBits = 5;
				SendPropNumBitsToGetNumBits = 6;
				SendPropTypes = SendPropEnums.OldNetPropTypes;
			} else {
				NetMsgTypeBits = Game == L4D1_1005 ? 5 : 6;
				SendPropNumBitsToGetNumBits = IsLeft4Dead() ? 6 : 7;
				SendPropTypes = SendPropEnums.NewNetPropTypes;
			}
			SendPropTypesReverseLookup = SendPropTypes.CreateReverseLookupDict();

			switch (h.DemoProtocol) {
				case 2:
					NewDemoProtocol = false;
					SendPropFlagBits = 11;
					goto default; // I don't know any other constants for demo protocol 2 so just cry
				case 3:
				case 4 when IsLeft4Dead1():
					NewDemoProtocol = IsLeft4Dead1();
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
					else if (Game == L4D1_1005)
						MessageTypes = DemoMessage.L4D1OldMessageList;
					else if (Game == L4D1_1040)
						MessageTypes = DemoMessage.L4D1SteamMessageList;
					else
						MessageTypes = DemoMessage.SteamPipeMessageList;
					break;
				case 4:
					NewDemoProtocol = true;
					SendPropFlagBits = 19;
					SoundFlagBits = 13; // 9?
					UserMessageLengthBits = IsLeft4Dead() ? 11 : 12;
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


		public bool IsLeft4Dead() => Game >= L4D1_1005 && Game <= L4D2_2220;

		public bool IsLeft4Dead1() => Game >= L4D1_1005 && Game <= L4D1_1040;

		public bool IsPortal1() => Game >= PORTAL_1_3420 && Game <= PORTAL_1_1910503;

		public bool IsHL2() => Game == HL2_OE;
	}


	// keep individual games grouped together and order them by release date
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	public enum SourceGame {
		HL2_OE, // todo add regular hl2

		PORTAL_1_3420,
		PORTAL_1_5135,
		PORTAL_1_1910503, // latest steam version

		PORTAL_2,

		L4D1_1005,
		L4D1_1040, // latest steam version

		L4D2_2000,
		L4D2_2012,
		L4D2_2027,
		L4D2_2042, // 2042 is protocol, version is 2063, 2075, or 2091 (thanks valve)
		L4D2_2220, // latest steam version
		UNKNOWN
	}


	// flags that get set during parsing, very WIP
	[Flags]
	public enum DemoParseResult {
		Success = 1,
		EntParsingEnabled = 2,
		UnknownGame = 4,
	}
}
