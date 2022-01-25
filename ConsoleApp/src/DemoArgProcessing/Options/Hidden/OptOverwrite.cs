using System.Collections.Immutable;
using ConsoleApp.GenericArgProcessing;

namespace ConsoleApp.DemoArgProcessing.Options.Hidden {

	public class OptOverwrite : DemoOption<bool> {

		public static readonly ImmutableArray<string> DefaultAliases = new[] {"--overwrite"}.ToImmutableArray();


		private static bool ParseBool(string s) {
			if (bool.TryParse(s, out bool res))
				return res;
			if (int.TryParse(s, out int intRes) && (intRes == 0 || intRes == 1))
				return intRes == 1;
			throw new ArgProcessUserException($"could not convert {s} to valid boolean");
		}


		public OptOverwrite() : base(
			DefaultAliases,
			Arity.ZeroOrOne,
			"If set, any options that create new demos will instead overwrite those demos." +
			$"\nFor such options this acts as a replacement for {OptOutputFolder.DefaultAliases[0]}." +
		   "\nThis is the only way to apply multiple edits to the same demo in a single pass.",
			"prompt",
			ParseBool,
			true, // default value must also be set in BaseDemoOption
			true) {}


		protected override void AfterParse(DemoParsingSetupInfo setupObj, bool arg, bool isDefault) {
			setupObj.OverWriteDemos = true;
			setupObj.OverwritePrompt = arg;
		}


		protected override void Process(DemoParsingInfo infoObj, bool arg, bool isDefault) {}


		protected override void PostProcess(DemoParsingInfo infoObj, bool arg, bool isDefault) {}
	}
}
