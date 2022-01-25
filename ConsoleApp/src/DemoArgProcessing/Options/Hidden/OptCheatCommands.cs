using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
			"Search for commands which are suspected to be cheat commands." +
			"\nNote that the list of commands is designed for portal 1 and is incomplete.",
			true) {}


		public override void AfterParse(DemoParsingSetupInfo setupObj) {
			setupObj.ExecutableOptions++;
		}


		public override void Process(DemoParsingInfo infoObj) {
			infoObj.PrintOptionMessage("searching for cheat commands");
			try {
				bool any = false;
				foreach (ConsoleCmd cmd in GetCheatCommands(infoObj.CurrentDemo)) {
					any = true;
					Console.WriteLine($"[{cmd.Tick}] {cmd.Command}");
				}
				if (!any)
					Console.WriteLine("no cheat commands found");
			} catch (Exception) {
				Utils.Warning("Search for cheat commands failed.\n");
			}
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
