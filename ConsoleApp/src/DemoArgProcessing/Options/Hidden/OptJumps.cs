using System;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using DemoParser.Parser.Components.Packets;
using DemoParser.Utils;

namespace ConsoleApp.DemoArgProcessing.Options.Hidden {

	public class OptJumps : DemoOption {

		public static readonly ImmutableArray<string> DefaultAliases = new[] {"--jumps"}.ToImmutableArray();


		public OptJumps() : base(
			DefaultAliases,
			$"Search for jump commands, equivalent to '{OptRegexSearch.DefaultAliases[0]} [-+]jump'",
			true) {}


		public override void AfterParse(DemoParsingSetupInfo setupObj) {
			setupObj.ExecutableOptions++;
		}


		// todo: implement legit jump verification and convert cmd num to key
		public override void Process(DemoParsingInfo infoObj) {
			infoObj.PrintOptionMessage("searching for jumps");
			try {
				bool any = false;
				foreach ((ConsoleCmd cmd, MatchCollection matches) in infoObj.CurrentDemo.CmdRegexMatches("[+-]jump")) {
					any = true;
					Console.WriteLine($"[{cmd.Tick}] {cmd.Command}");
				}
				if (!any)
					Console.WriteLine("no jumps found");
			} catch (Exception) {
				Utils.Warning("Search for jumps failed.\n");
			}
		}


		public override void PostProcess(DemoParsingInfo infoObj) {}
	}
}
