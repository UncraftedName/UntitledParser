using System;
using System.Collections.Immutable;
using System.IO;
using DemoParser.Utils;

namespace ConsoleApp.DemoArgProcessing.Options {

	public class OptDemoDump : DemoOption {

		public static readonly ImmutableArray<string> DefaultAliases = new[] {"--demo-dump", "-D"}.ToImmutableArray();


		public OptDemoDump() : base(DefaultAliases, "Create a text representation of all parsable data in demos") {}


		public override void AfterParse(DemoParsingSetupInfo setupObj) {
			setupObj.ExecutableOptions++;
		}


		public override void Process(DemoParsingInfo infoObj) {
			infoObj.PrintOptionMessage("writing demo dump");
			TextWriter tw = infoObj.StartWritingText("demo-dump");
			try {
				PrettyStreamWriter pw = new PrettyStreamWriter(((StreamWriter)tw).BaseStream);
				infoObj.CurrentDemo.PrettyWrite(pw);
				pw.Flush(); // see note at PrettyStreamWriter
			} catch (Exception) {
				Utils.Warning("Failed to create demo dump.\n");
			}
		}


		public override void PostProcess(DemoParsingInfo infoObj) {}
	}
}
