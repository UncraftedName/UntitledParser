using System.Collections.Immutable;
using System.IO;
using DemoParser.Utils;

namespace ConsoleApp.DemoArgProcessing.Options {
	
	public class OptDemoDump : DemoOption {
		
		public static readonly ImmutableArray<string> DefaultAliases = new[] {"--demo-dump", "-D"}.ToImmutableArray();
		
		
		public OptDemoDump() : base(DefaultAliases, "create a text representation of all parsable data in demos") {}
		
		
		public override void AfterParse(DemoParsingSetupInfo setupObj) {
			setupObj.ExecutableOptions++;
			setupObj.FolderOutputRequired = true;
		}


		public override void Process(DemoParsingInfo infoObj) {
			TextWriter tw = infoObj.InitTextWriter("writing demo dump", "demo-dump");
			PrettyStreamWriter pw = new PrettyStreamWriter(((StreamWriter)tw).BaseStream);
			infoObj.CurrentDemo.PrettyWrite(pw);
			pw.Flush(); // see note at PrettyStreamWriter
		}


		public override void PostProcess(DemoParsingInfo infoObj) {}
	}
}
