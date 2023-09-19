using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using ConsoleApp.DemoArgProcessing.Options;
using ConsoleApp.DemoArgProcessing.Options.Hidden;
using ConsoleApp.GenericArgProcessing;
using DemoParser.Parser;

namespace ConsoleApp.DemoArgProcessing {

	public class DemoParserSubCommand : BaseSubCommand<DemoParsingSetupInfo, DemoParsingInfo> {

		private readonly List<FileSystemInfo> _argPaths;
		public ICollection<FileSystemInfo> ArgPaths => _argPaths;
		private readonly SortedSet<FileInfo> _demoPaths;
		public ICollection<FileInfo> DemoPaths => _demoPaths;
		public override string VersionString => Utils.GetVersionString() ?? "could not determine version";
		public override string UsageString => $"{Utils.GetExeName()} <demos/dirs> [options]";


		public DemoParserSubCommand(IImmutableList<BaseOption<DemoParsingSetupInfo, DemoParsingInfo>> options)
			: base(options)
		{
			_argPaths = new List<FileSystemInfo>();
			_demoPaths = new SortedSet<FileInfo>(new AlphanumComparatorFileInfo());
		}


		// assume this is a demo file or a folder of files
		protected override bool ParseDefaultArgument(string arg, out string? failReason) {
			if (File.Exists(arg)) {
				FileInfo fi = new FileInfo(arg);
				if (fi.Extension == ".dem") {
					_argPaths.Add(new FileInfo(arg));
				} else {
					failReason = $"File \"{arg}\" is not a valid demo file";
					return false;
				}
			} else if (Directory.Exists(arg)) {
				_argPaths.Add(new DirectoryInfo(arg));
			} else {
				failReason = $"\"{arg}\" is not a valid path";
				return false;
			}
			failReason = null;
			return true;
		}


		protected override void Reset() {
			base.Reset();
			_argPaths.Clear();
			_demoPaths.Clear();
		}


		private void EnableTimeOptionDefault(DemoParsingSetupInfo setupInfo) {
			if (TryGetOption(OptTime.DefaultAliases[0], out var option)) {
				option.Enable(null);
				option.AfterParse(setupInfo);
				// Due to the portal community's love for hundreds of saveloads, the time option will time w/ first tick by default
				// for p1 and l4d (because hindsight or something). This is a hack for now and will hopefully by improved with a
				// more advanced timing system in the utopian future.
				((OptTime)option).SetForceFirstTickTiming(new[] {
					SourceGame.L4D1_1005, SourceGame.L4D1_1040, SourceGame.L4D2_2000, SourceGame.L4D2_2011,
					SourceGame.L4D2_2012, SourceGame.L4D2_2027, SourceGame.L4D2_2042, SourceGame.L4D2_2091, SourceGame.L4D2_2147,
					SourceGame.L4D2_2203, SourceGame.PORTAL_1_3420, SourceGame.PORTAL_1_5135, SourceGame.PORTAL_1_1910503
				});
			} else {
				throw new ArgProcessProgrammerException("listdemo option not passed to demo sub-command.");
			}
		}


		private static void PromptOverwrite(DemoParsingSetupInfo setupInfo) {
			// if (setupInfo.OverWriteDemos && !setupInfo.WritesNewDemos) <- I could add a check for this, is it necessary?
			if (setupInfo.OverWriteDemos && setupInfo.EditsDemos && setupInfo.OverwritePrompt) {
				Utils.Warning("Warning:");
				Console.WriteLine($" '{OptOverwrite.DefaultAliases[0]}' enabled, this will edit demos and may cause corruption!");
				Console.Write("Are you sure you want to continue? ");
				string? inp = null;
				while (inp != "y") {
					Console.Write("[y/n]");
					inp = Console.ReadLine()?.Trim().ToLower();
					if (inp == "n")
						return;
				}
			}
		}


		private void FlattenDirectories(DemoParsingSetupInfo setupInfo) {
			// flatten directories/files into just files
			foreach (FileSystemInfo fileSystemInfo in _argPaths) {
				switch (fileSystemInfo) {
					case FileInfo fi:
						_demoPaths.Add(fi);
						break;
					case DirectoryInfo di:
						_demoPaths.UnionWith(Directory.GetFiles(di.FullName, "*.dem",
								setupInfo.ShouldSearchForDemosRecursively ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
							.Select(s => new FileInfo(s)));
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
			if (_demoPaths.Count == 0) {
				if (_argPaths.Any(fsi => fsi is DirectoryInfo))
					throw new ArgProcessUserException($"no demos found, maybe try searching subfolders using '{OptRecursive.DefaultAliases[0]}'.");
				throw new ArgProcessUserException("no demos found!");
			}
		}


		private IEnumerable<(FileInfo demoPath, string displayName)> CreateDisplayStrings() {
			// Shorten the paths of the demos if possible, the shared path between the first and last paths will give
			// the overall shared path of everything. If it's empty then we know the demos span multiple drives.
			string commonParent = Utils.SharedPathSubstring(_demoPaths.Min!.FullName, _demoPaths.Max!.FullName);
			return _demoPaths.Select(demoPath => (
				demoPath,
				commonParent == ""
					? demoPath.FullName
					: Path.GetRelativePath(commonParent, demoPath.FullName)
				));
		}


		public override void Execute(params string[] args) {
			if (CheckHelpAndVersion(args))
				return;
			DemoParsingSetupInfo setupInfo = new DemoParsingSetupInfo();
			ParseArgs(args, setupInfo);

			// enable --time implicitly if there are no other options
			if (setupInfo.ExecutableOptions == 0)
				EnableTimeOptionDefault(setupInfo);

			PromptOverwrite(setupInfo);
			FlattenDirectories(setupInfo);
			using DemoParsingInfo parsingInfo = new DemoParsingInfo(setupInfo, CreateDisplayStrings().ToImmutableList());
			Process(parsingInfo);
		}
	}
}
