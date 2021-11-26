using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DemoParser.Parser;
using DemoParser.Parser.Components.Packets;
using DemoParser.Utils;

namespace ConsoleApp.DemoArgProcessing.Options.Hidden {
	
	public class OptCheatCommands : DemoOption {
		
		public static readonly ImmutableArray<string> DefaultAliases = new[] {"--cheat-commands"}.ToImmutableArray();
		
		
		public OptCheatCommands() : base(
			DefaultAliases,
			"Searches for commands which are suspected to be cheat commands." +
			"\nNote that the list of commands is designed for portal 1 and is incomplete.",
			true) {}
		
		
		public override void AfterParse(DemoParsingSetupInfo setupObj) {
			setupObj.ExecutableOptions++;
		}


		public override void Process(DemoParsingInfo infoObj) {
			TextWriter tw = infoObj.StartWritingText("searching for cheat commands", "cheat-commands");
			bool any = false;
			foreach (ConsoleCmd cmd in GetCheatCommands(infoObj.CurrentDemo)) {
				any = true;
				tw.WriteLine($"[{cmd.Tick}] {cmd.Command}");
			}
			if (!any)
				tw.WriteLine("no cheat commands found");
		}


		public static IEnumerable<ConsoleCmd> GetCheatCommands(SourceDemo demo) {
			Regex r = new Regex(new[] {
				"host_timescale", "god", "sv_cheats +1", "buddha", "host_framerate", "sv_accelerate", "gravity", 
				"sv_airaccelerate", "noclip", "impulse", "ent_", "sv_gravity", "upgrade_portalgun", 
				"phys_timescale", "notarget", "give", "fire_energy_ball", "_spt_", "plugin_load", 
				"!picker", "(?<!swit)ch_", "tas_", "r_", "sv_", "mat_"
			}.SequenceToString(")|(", "(", ")"), RegexOptions.IgnoreCase);
			return demo.CmdRegexMatches(r).Select(t => t.cmd);
		}


		public override void PostProcess(DemoParsingInfo infoObj) {}
	}
}
