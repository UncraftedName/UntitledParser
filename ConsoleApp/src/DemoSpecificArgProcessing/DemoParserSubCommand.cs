using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using ConsoleApp.GenericArgProcessing;

namespace ConsoleApp.DemoSpecificArgProcessing {
	
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
					throw new ArgProcessException("");
			} else if (Directory.Exists(arg)) {
				_argPaths.Add(new DirectoryInfo(arg));
			} else {
				throw new ArgProcessException($"\"{arg}\" is not a valid file or directory!");
			}
		}


		public override void Reset() {
			base.Reset();
			_argPaths.Clear();
			_demoPaths.Clear();
		}


		public void Execute(params string[] args) => Execute(0, args);


		public void Execute(int startIndex, params string[] args) {
			Reset();
			DemoParsingSetupInfo setupInfo = new DemoParsingSetupInfo();
			ParseArgs(args, setupInfo, startIndex);
			if (setupInfo.ExecutableOptions == 0) {
				// TODO
			}
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
				throw new ArgProcessException("No demos found!");
			// Shorten the paths of the demos if possible, the shared path between the first and last paths will give
			// the overall shared path of everything. If it's empty then we know the demos span multiple drives.
			string commonParent = Utils.SharedPathSubstring(_demoPaths.Min.FullName, _demoPaths.Max.FullName);
			var paths =
				from demoPath in _demoPaths
				let displayName = commonParent == ""
					? demoPath.FullName
					: PathExt.GetRelativePath(commonParent, demoPath.FullName)
				select (demoPath, displayName);
			DemoParsingInfo parsingInfo = new DemoParsingInfo(setupInfo, paths.ToImmutableList());
			// run
			Process(parsingInfo);
		}
	}
}
