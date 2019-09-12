using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UncraftedDemoParser.Parser;

namespace UncraftedDemoParser.Utils {
	
	// this is a huge fucking mess, i'll fix this up eventually
	public class ConsoleOptions {

		private const string FolderName = "Verbose Output";
		private List<string> Directories;
		
		public bool Verbose;
		public bool CountSvcMessages;
		public bool DumpSvcPackets;
		public bool QuietMode;
		public bool SortAllDemos;
		public bool StitchGuesser;


		public ConsoleOptions(string[] args) {
			try {
				Directories = new List<string>();
				int i = 0;
				for (i = 0; i < args.Length; i++) {
					if (args[i][0] == '/' || args[i][0] == '-')
						break;
					Directories.Add(args[i]);
				}
				for (int optionIndex = i; optionIndex < args.Length; optionIndex++) {
					switch (Char.ToLower(args[optionIndex][1])) {
						case 'v':
							Verbose = true;
							break;
						case 'c':
							CountSvcMessages = true;
							break;
						case 'p':
							DumpSvcPackets = true;
							break;
						case 'q':
							QuietMode = true;
							break;
						case 's':
							SortAllDemos = true;
							break;
						case 't':
							StitchGuesser = true;
							break;
						default:
							throw new ArgumentException();
					}
				}
			} catch (ArgumentException e) {
				Console.WriteLine(e.ToString());
				PrintHelpAndExit();
			}
		}


		public void ProcessOptions() {
			if (Directories.Count == 0) {
				Console.WriteLine("Usage: CommandDumper.exe [input file/directories] <options>");
				Environment.Exit(0);
			}
			try {
				List<DirectoryInfo> directoryArgs = Directories.Select(s => new DirectoryInfo(s)).ToList();
				List<DirectoryInfo> demoDirs = new List<DirectoryInfo>();
				foreach (DirectoryInfo directoryArg in directoryArgs) {
					if (File.GetAttributes(directoryArg.FullName).HasFlag(FileAttributes.Directory))
						demoDirs.AddRange(directoryArg.GetFiles().ToList()
							.Where(info => Path.GetExtension(info.FullName).Equals(".dem"))
							.Select(info => new DirectoryInfo(info.FullName)));
					else
						demoDirs.Add(directoryArg);
				}
				if (SortAllDemos)
					demoDirs.Sort((info1,  info2) => info1.Name.CompareTo(info2.Name));
				List<SourceDemo> demos = demoDirs.Select(info => new SourceDemo(info, parse: false)).ToList();
				Console.ForegroundColor = ConsoleColor.White;
				Console.WriteLine("\nCustom source demo parser by UncraftedName\n");
				if (Verbose || DumpSvcPackets || CountSvcMessages)
					Directory.CreateDirectory($"{Directory.GetCurrentDirectory()}/{FolderName}");
				if (demos.Count == 1) {
					if (Verbose || DumpSvcPackets || CountSvcMessages)
						demos[0].ParseBytes();
					else
						demos[0].QuickParse();
					demos[0].PrintListdemoOutput(!QuietMode, false);
					if (DumpSvcPackets)
						demos[0].SvcMessagesCounterAsString().WriteToFiles(
							$"{Directory.GetCurrentDirectory()}/{FolderName}/_svc message counter.txt");
					if (Verbose)
						demos[0].AsVerboseString().WriteToFiles($"{Directory.GetCurrentDirectory()}/{FolderName}/_verbose.txt");
					if (DumpSvcPackets)
						demos[0].SvcMessagesAsString().WriteToFiles(
							$"{Directory.GetCurrentDirectory()}/{FolderName}/_svc messages dump.txt");
				} else {
					for (int i = 0; i < demos.Count; i++) {
						string demoName = Path.GetFileNameWithoutExtension(demoDirs[i].Name);
						if (Verbose || DumpSvcPackets || CountSvcMessages)
							demos[i].ParseBytes();
						else
							demos[i].QuickParse();
						demos[i].PrintListdemoOutput(!QuietMode, true);
						if (DumpSvcPackets)
							demos[i].SvcMessagesCounterAsString().WriteToFiles(
								$"{Directory.GetCurrentDirectory()}/{FolderName}/svc message counter - {demoName}.txt");
						if (Verbose)
							demos[i].AsVerboseString().WriteToFiles(
								$"{Directory.GetCurrentDirectory()}/{FolderName}/verbose - {demoName}.txt");
						if (DumpSvcPackets)
							demos[i].SvcMessagesAsString().WriteToFiles(
								$"{Directory.GetCurrentDirectory()}/{FolderName}/svc messages dump - {demoName}.txt");
					}
				}

				Console.ReadKey();
			} catch (Exception e) {
				Console.WriteLine(e.ToString());
				PrintHelpAndExit();
			}
		}
		

		public static void PrintHelpAndExit() {
			Console.ForegroundColor = ConsoleColor.Gray;
			Console.Write(
				"Usage: CommandDumper.exe [input file/directories] <options>\n\n" +
				"Displays various information about the provided demos and optionally dumps verbose information " +
				$"to text files to a new folder called \"{FolderName}\". " +
				"If several demos are provided the ordering used is the same as the order given. " +
				"If a directory is given, all demos in that directory are ordered based on filename.\n" +
				$"\n  {"-v",-7}dumps verbose information" +
				$"\n  {"-c",-7}dumps amount of svc messages" +
				$"\n  {"-p",-7}dumps all svc packets" +
				$"\n  {"-q",-7}don't display demo header in console" +
				$"\n  {"-s",-7}sort all given demos based on filename first" +
				$"\n  {"-t",-7}guess the order of the provided demos [TODO]\n");
			Environment.Exit(0);
		}
	}
}