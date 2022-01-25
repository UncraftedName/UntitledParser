using System;
using System.Collections.Immutable;
using System.IO;
using ConsoleApp.GenericArgProcessing;

namespace ConsoleApp.DemoArgProcessing.Options {

	public class OptOutputFolder : DemoOption<string> {

		public static readonly ImmutableArray<string> DefaultAliases = new[] {"--output-folder", "-o"}.ToImmutableArray();


		public OptOutputFolder() : base(
			DefaultAliases,
			Arity.One,
			"If set, any options that generate files will output to this folder; useful when checking many demos",
			"path",
			ValidateFolderName,
			".") {}


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
