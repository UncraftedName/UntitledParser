using System;
using System.Collections.Immutable;
using System.IO;
using ConsoleApp.GenericArgProcessing;

namespace ConsoleApp.DemoArgProcessing.Options {
	
	public class OptFolderOut : DemoOption<string> {

		public static readonly ImmutableArray<string> DefaultAliases = new[] {"--folder", "-f"}.ToImmutableArray();
		
		
		public OptFolderOut()
			: base(DefaultAliases, Arity.ZeroOrOne, "specifies a folder to redirect", ValidateFolderName, ".") {}


		private static string ValidateFolderName(string name) {
			try {
				string _ = Path.GetFullPath(name);
			} catch (Exception) {
				throw new ArgProcessUserException($"\"{name}\" is not a valid folder output.");
			}
			return name;
		}


		protected override void AfterParse(DemoParsingSetupInfo setupObj, string arg, bool isDefault) {
			setupObj.FolderOutput = arg;
		}


		protected override void Process(DemoParsingInfo infoObj, string arg, bool isDefault) {}
		protected override void PostProcess(DemoParsingInfo infoObj, string arg, bool isDefault) {}
	}
}