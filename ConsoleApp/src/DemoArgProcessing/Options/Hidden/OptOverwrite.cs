using System.Collections.Immutable;

namespace ConsoleApp.DemoArgProcessing.Options.Hidden {
	
	public class OptOverwrite : DemoOption {
		
		public static readonly ImmutableArray<string> DefaultAliases = new[] {"--overwrite"}.ToImmutableArray();
		
		public OptOverwrite() : base(
			DefaultAliases,
			"If set, any options that create new demos will instead overwrite those demos." +
			$"\nFor such options this acts as a replacement for {OptOutputFolder.DefaultAliases[0]}." +
		   "\nThis is the only way to apply multiple edits to the same demo in a single pass.",
			true) {}
		
		
		public override void AfterParse(DemoParsingSetupInfo setupObj) {
			setupObj.OverWriteDemos = true;
		}


		public override void Process(DemoParsingInfo infoObj) {}


		public override void PostProcess(DemoParsingInfo infoObj) {}
	}
}
