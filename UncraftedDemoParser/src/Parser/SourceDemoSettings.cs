using System;
using UncraftedDemoParser.Parser.Components;
using static UncraftedDemoParser.Parser.SourceDemoSettings.SourceGame;

namespace UncraftedDemoParser.Parser {
	
	// determines most of the demo settings based solely on the header
	public class SourceDemoSettings {

		public enum SourceGame {
			Portal1,
			Portal2
		}
		
		
		public readonly bool NewEngine;
		public readonly SourceGame Game;
		public readonly int MaxSplitscreenPlayers;
		public bool HasAlignmentByte => NewEngine;
		public readonly float TicksPerSeoncd;
		public readonly Header Header;


		public SourceDemoSettings(Header h) {
			Header = h;
			if (h.DemoProtocol == 3 && h.NetworkProtocol == 15)
				Game = Portal1;
			else if (h.DemoProtocol == 4 && h.NetworkProtocol == 2001)
				Game = Portal2;
			else
				Console.WriteLine("\nUnknown game, demo might not parse properly. Update in SourceDemoSettings.\n");

			switch (Game) {
				case Portal1:
					MaxSplitscreenPlayers = 1;
					NewEngine = false;
					TicksPerSeoncd = 200 / 3.0f;
					break;
				case Portal2:
					MaxSplitscreenPlayers = 2;
					NewEngine = true;
					TicksPerSeoncd = 60;
					break;
				default:
					MaxSplitscreenPlayers = 4;
					NewEngine = true;
					TicksPerSeoncd = 60;
					break;
			}
		}
	}
}