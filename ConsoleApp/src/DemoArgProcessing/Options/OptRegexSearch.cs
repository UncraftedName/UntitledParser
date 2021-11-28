using System;
using System.Collections.Immutable;
using System.IO;
using System.Text.RegularExpressions;
using ConsoleApp.GenericArgProcessing;
using DemoParser.Parser.Components.Packets;
using DemoParser.Utils;

namespace ConsoleApp.DemoArgProcessing.Options {
	
	// I can use Regex as the template since it has a simple ToString() implementation
	public class OptRegexSearch : DemoOption<Regex> {
		
		public static readonly ImmutableArray<string> DefaultAliases = new[] {"--regex", "-R"}.ToImmutableArray();


		private static Regex RegexValidator(string s) {
			try {
				return new Regex(s, RegexOptions.IgnoreCase);
			} catch (Exception e) {
				throw new ArgProcessUserException($"invalid regex ({e.Message})");
			}
		}


		public OptRegexSearch() : base(
			DefaultAliases,
			Arity.ZeroOrOne,
			"Find commands that match the given regex (case insensitive), the default pattern will match every command",
			"pattern",
			 RegexValidator,
			new Regex(".*")) {}


		protected override void AfterParse(DemoParsingSetupInfo setupObj, Regex r, bool isDefault) {
			setupObj.ExecutableOptions++;
		}


		protected override void Process(DemoParsingInfo infoObj, Regex r, bool isDefault) {
			TextWriter tw = infoObj.StartWritingText("looking for regex matches", "regex");
			try {
				bool any = false;
				foreach ((ConsoleCmd cmd, MatchCollection _) in infoObj.CurrentDemo.CmdRegexMatches(r)) {
					any = true;
					tw.WriteLine($"[{cmd.Tick}] {cmd.Command}");
				}
				if (!any)
					tw.WriteLine("no matches found");
			} catch (Exception) {
				Utils.Warning("Regex match search failed.\n");
			}
		}


		protected override void PostProcess(DemoParsingInfo infoObj, Regex arg, bool isDefault) {}
	}
}
