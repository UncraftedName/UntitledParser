using System;
using System.Diagnostics.CodeAnalysis;
using UncraftedDemoParser.Parser.Components;
using static UncraftedDemoParser.Parser.SourceDemoSettings.SourceGame;

namespace UncraftedDemoParser.Parser {
	
	// determines most of the demo settings based solely on the header
	public class SourceDemoSettings {

		[SuppressMessage("ReSharper", "InconsistentNaming")]
		public enum SourceGame {
			PORTAL_1,
			PORTAL_1_3420,
			PORTAL_1_STEAMPIPE,
			PORTAL_2,
			UNKNOWN
		}
		
		
		public readonly bool NewEngine;
		public readonly SourceGame Game;
		public readonly int MaxSplitscreenPlayers;
		public bool HasAlignmentByte => NewEngine;
		public readonly float TicksPerSecond;
		public readonly Header Header;


		public SourceDemoSettings(Header h) {
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
				default:
					Game = UNKNOWN;
					Console.WriteLine("\nUnknown game, demo might not parse properly. Update in SourceDemoSettings.\n");
					break;
			}

			switch (Game) {
				case PORTAL_1:
				case PORTAL_1_3420:
				case PORTAL_1_STEAMPIPE:
					MaxSplitscreenPlayers = 1;
					NewEngine = false;
					TicksPerSecond = 200 / 3.0f;
					break;
				case PORTAL_2:
					MaxSplitscreenPlayers = 2;
					NewEngine = true;
					TicksPerSecond = 60;
					break;
				default:
					MaxSplitscreenPlayers = 4;
					NewEngine = true;
					TicksPerSecond = 60;
					break;
			}
		}
	}
}