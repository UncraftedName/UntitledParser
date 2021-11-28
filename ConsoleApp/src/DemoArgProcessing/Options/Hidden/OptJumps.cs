using System;
using System.Collections.Immutable;
using System.IO;
using System.Text.RegularExpressions;
using DemoParser.Parser.Components.Packets;
using DemoParser.Utils;

namespace ConsoleApp.DemoArgProcessing.Options.Hidden {
	
	public class OptJumps : DemoOption {
		
		public static readonly ImmutableArray<string> DefaultAliases = new[] {"--jumps"}.ToImmutableArray();
		
		
		public OptJumps() : base(
			DefaultAliases,
			$"Searches for jump commands, equivalent to '{OptRegexSearch.DefaultAliases[0]} [-+]jump'",
			true) {}
		
		
		public override void AfterParse(DemoParsingSetupInfo setupObj) {
			setupObj.ExecutableOptions++;
		}
		
		
		// todo: implement legit jump verification and convert cmd num to key
		public override void Process(DemoParsingInfo infoObj) {
			try {
				TextWriter tw = infoObj.StartWritingText("searching for jumps", "jumps");
				bool any = false;
				foreach ((ConsoleCmd cmd, MatchCollection matches) in infoObj.CurrentDemo.CmdRegexMatches("[+-]jump")) {
					any = true;
					tw.WriteLine($"[{cmd.Tick}] {cmd.Command}");
				}
				if (!any)
					tw.WriteLine("no jumps found");
			} catch (Exception) {
				Utils.WriteColor("Search for jumps failed.\n", ConsoleColor.Red);
			}
		}


		public override void PostProcess(DemoParsingInfo infoObj) {}
	}
}
