using System.Collections.Immutable;

namespace ConsoleApp.DemoSpecificArgProcessing.Options {
	
	public class OptRecursive : DemoOption {

		public static ImmutableArray<string> DefaultAliases = new[] {"--recursive", "-r"}.ToImmutableArray();
		
		
		public OptRecursive() : base(DefaultAliases, "specifies if the search for demos should be recursive") {}
		
		
		public override void AfterParse(DemoParsingSetupInfo setupObj) {
			setupObj.ShouldSearchForDemosRecursively = true;
		}


		public override void Process(DemoParsingInfo infoObj) {}
		public override void PostProcess(DemoParsingInfo infoObj) {}
	}
}