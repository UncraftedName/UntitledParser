using System;
using System.Diagnostics.CodeAnalysis;
using DemoParser.Parser.Components;
using static DemoParser.Parser.SourceDemoSettings.SourceGame;

namespace DemoParser.Parser {
	
	// determines most of the demo settings based solely on the header
	public class SourceDemoSettings {

		[SuppressMessage("ReSharper", "InconsistentNaming")]
		public enum SourceGame {
			PORTAL_1,
			PORTAL_1_3420, // might be equivalent to leak
			PORTAL_1_STEAMPIPE,
			PORTAL_2,
			L4D2_2000,
			UNKNOWN
		}


		public bool NewEngine => Header.DemoProtocol == 4;
		public readonly SourceGame Game;
		public readonly int MaxSplitscreenPlayers;
		public bool HasPlayerSlot => NewEngine; // "alignment byte" in nekz' parser
		public readonly DemoHeader Header;
		// constants, some may depend on the game
		public int NetMsgTypeBits => Header.NetworkProtocol == 14 ? 5 : 6;
		public int UserMessageLengthBits => NewEngine && Game != L4D2_2000 ? 12 : 11;
		public int MaxEdictBits => 11;
		public int MaxUserDataBits => 14;
		public float TickInterval; // to be determined while parsing in SvcServerInfo


		public SourceDemoSettings(DemoHeader h) {
			Header = h;
			switch (h.DemoProtocol) {
				case 3 when h.NetworkProtocol == 14:
					Game = PORTAL_1_3420;
					break;
				case 3 when h.NetworkProtocol == 15:
					Game = PORTAL_1;
					break;
				case 3 when h.NetworkProtocol == 24:
					Game = PORTAL_1_STEAMPIPE;
					break;
				case 4 when h.NetworkProtocol == 2001:
					Game = PORTAL_2;
					break;
				case 4 when h.NetworkProtocol == 2042:
					Game = L4D2_2000;
					break;
				default:
					Game = UNKNOWN;
					Console.WriteLine($"\nUnknown game, demo might not parse properly. Update in {GetType().FullName}.\n");
					break;
			}

			switch (Game) {
				case PORTAL_1:
				case PORTAL_1_3420:
				case PORTAL_1_STEAMPIPE:
					MaxSplitscreenPlayers = 1;
					break;
				case PORTAL_2:
					MaxSplitscreenPlayers = 2;
					break;
				case L4D2_2000:
					MaxSplitscreenPlayers = 4;
					break;
				default:
					MaxSplitscreenPlayers = 4;
					break;
			}
		}
	}
}