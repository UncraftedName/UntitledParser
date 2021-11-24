using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using DemoParser.Parser;
using DemoParser.Parser.Components.Messages.UserMessages;
using DemoParser.Utils;

namespace ConsoleApp.DemoArgProcessing.Options {
	
	public class OptPortals : DemoOption {
		
		public static readonly ImmutableArray<string> DefaultAliases = new[] {"--portals-fired", "-P"}.ToImmutableArray();
		
		
		public OptPortals() : base(
			DefaultAliases,
			"Searches for portals that have been fired from the player's portal gun",
			true) {}
		
		
		public override void AfterParse(DemoParsingSetupInfo setupObj) {
			setupObj.ExecutableOptions++;
		}


		// todo check if these are on cap
		public override void Process(DemoParsingInfo infoObj) {
			TextWriter tw = infoObj.InitTextWriter("searching for portals fired by player", "portals");
			bool any = false;
			foreach ((Rumble userMessage, int tick) in GetPortalsFiredByPlayer(infoObj.CurrentDemo)) {
				any = true;
				switch (userMessage.RumbleType) {
					case RumbleLookup.PortalgunLeft:
						Utils.PushForegroundColor(ConsoleColor.Cyan);
						tw.WriteLine($"[{tick}] BLUE PORTAL fired by player");
						break;
					case RumbleLookup.PortalgunRight:
						Utils.PushForegroundColor(ConsoleColor.Red); // closest we've got is red :/
						tw.WriteLine($"[{tick}] ORANGE PORTAL fired by player");
						break;
					case RumbleLookup.PortalPlacementFailure:
						Utils.PushForegroundColor(Console.ForegroundColor); // unchanged
						tw.WriteLine($"[{tick}] portal fired and missed");
						break;
					default:
						throw new ArgProcessProgrammerException($"invalid rumble type: {userMessage.RumbleType}");
				}
				Utils.PopForegroundColor();
			}
			if (!any)
				tw.WriteLine("no portals fired by player");
		}

		
		public static IEnumerable<(Rumble userMessage, int tick)> GetPortalsFiredByPlayer(SourceDemo demo) {
			return demo.FilterForUserMessage<Rumble>().Where(t =>
				t.userMessage.RumbleType == RumbleLookup.PortalgunLeft ||
				t.userMessage.RumbleType == RumbleLookup.PortalgunRight ||
				t.userMessage.RumbleType == RumbleLookup.PortalPlacementFailure);
		}


		public override void PostProcess(DemoParsingInfo infoObj) {}
	}
}
