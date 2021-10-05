using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using ConsoleApp.DemoArgProcessing.Options;
using ConsoleApp.GenericArgProcessing;

namespace ConsoleApp.DemoArgProcessing {
	
	public class DemoParserSubCommand : BaseSubCommand<DemoParsingSetupInfo, DemoParsingInfo> {
		
		private readonly List<FileSystemInfo> _argPaths;
		public ICollection<FileSystemInfo> ArgPaths => _argPaths;
		private readonly SortedSet<FileInfo> _demoPaths;
		public ICollection<FileInfo> DemoPaths => _demoPaths;


		public DemoParserSubCommand(IImmutableList<BaseOption<DemoParsingSetupInfo, DemoParsingInfo>> options)
			: base(options)
		{
			_argPaths = new List<FileSystemInfo>();
			_demoPaths = new SortedSet<FileInfo>(new AlphanumComparatorFileInfo());
		}
		
		
		protected override void ParseDefaultArgument(string arg) {
			if (File.Exists(arg)) {
				FileInfo fi = new FileInfo(arg);
				if (fi.Extension == ".dem")
					_argPaths.Add(new FileInfo(arg));
				else
					throw new ArgProcessUserException($"File \"{arg}\" is not a valid demo file.");
			} else if (Directory.Exists(arg)) {
				_argPaths.Add(new DirectoryInfo(arg));
			} else {
				throw new ArgProcessUserException($"\"{arg}\" is not a valid file or directory!");
			}
		}


		protected override void Reset() {
			base.Reset();
			_argPaths.Clear();
			_demoPaths.Clear();
		}


		public void Execute(params string[] args) => Execute(0, args);


		public void Execute(int startIndex, params string[] args) {
			DemoParsingSetupInfo setupInfo = new DemoParsingSetupInfo();
			ParseArgs(args, setupInfo, startIndex);
			// check if we have/need a folder output
			if (setupInfo.FolderOutputRequired && setupInfo.FolderOutput == null)
				throw new ArgProcessUserException($"Folder output is required, use {OptFolderOut.DefaultAliases[0]} to set a path.");
			// enable listdemo if there are no other options
			if (TotalEnabledOptions == 0) {
				if (TryGetOption(OptListdemo.DefaultAliases[0], out var option)) {
					option.Enable(setupInfo, null);
					option.AfterParse(setupInfo);
				} else {
					throw new ArgProcessProgrammerException("Listdemo option not passed to constructor, no default option.");
				}
			}
			if (setupInfo.ExecutableOptions == 0)
				throw new ArgProcessUserException("No executable options given!");
			// flatten directories/files into just files
			foreach (FileSystemInfo fileSystemInfo in _argPaths) {
				switch (fileSystemInfo) {
					case FileInfo fi:
						_demoPaths.Add(fi);
						break;
					case DirectoryInfo di:
						_demoPaths.UnionWith(Directory.GetFiles(di.Name, "*.dem,",
							setupInfo.ShouldSearchForDemosRecursively ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
							.Select(s => new FileInfo(s)));
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
			if (_demoPaths.Count == 0)
				throw new ArgProcessUserException("No demos found!");
			// Shorten the paths of the demos if possible, the shared path between the first and last paths will give
			// the overall shared path of everything. If it's empty then we know the demos span multiple drives.
			string commonParent = Utils.SharedPathSubstring(_demoPaths.Min.FullName, _demoPaths.Max.FullName);
			var paths =
				from demoPath in _demoPaths
				let displayName = commonParent == ""
					? demoPath.FullName
					: PathExt.GetRelativePath(commonParent, demoPath.FullName)
				select (demoPath, displayName);
			using DemoParsingInfo parsingInfo = new DemoParsingInfo(setupInfo, paths.ToImmutableList());
			Process(parsingInfo);
		}
	}
}
