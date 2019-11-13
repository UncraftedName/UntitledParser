using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UncraftedDemoParser.Parser.Components.Abstract;
using UncraftedDemoParser.Parser.Components.Packets;
using UncraftedDemoParser.Utils;

namespace UncraftedDemoParser.ConsoleParsing {
	
	// I tried to make this better than it was. I'm not sure if I did, but at least I made some tasty spaghetti.
	internal static partial class ConsoleOptions {

		private const string Version = "0.3";
		
		// A list of all the options; each option has the following properties:
		// the character to set the option, the priority of that option:
		// 		(5 means dump/print option, anything else means to run once before the demo parsing begins),
		// whether that option allows a string after it, the function for that option,
		// the file name to dump the output to, and the packets to parse for that option to work.
		private static readonly Dictionary<char, Tuple<int, bool, Action, string, Type[]>> OptionList = new Dictionary<char, Tuple<int, bool, Action, string, Type[]>> {
			{'f', Tuple.Create<int, bool, Action, string, Type[]>(8, true,  SetOutputToFile, 	 null, 				new Type[0])},
			{'s', Tuple.Create<int, bool, Action, string, Type[]>(6, false, SortDemos, 			 null, 				new Type[0])},
			{'h', Tuple.Create<int, bool, Action, string, Type[]>(9, false, PrintOptionHelp,	 null, 				new Type[0])},
			{'l', Tuple.Create<int, bool, Action, string, Type[]>(5, false, PrintListDemoOutput, "listdemo output", new[] {typeof(ConsoleCmd)})},
			{'v', Tuple.Create<int, bool, Action, string, Type[]>(5, false, PrintVerboseOutput,  "verbose output",	new[] {typeof(DemoPacket)})}, // parse all packets
			{'m', Tuple.Create<int, bool, Action, string, Type[]>(5, false, DumpSvcMessages, 	 "svc messages", 	new[] {typeof(Packet)})},
			{'c', Tuple.Create<int, bool, Action, string, Type[]>(5, false, DumpCheats,	 		 "cheats", 			new[] {typeof(ConsoleCmd)})},
			{'p', Tuple.Create<int, bool, Action, string, Type[]>(5, false, DumpPositions, 		 "positions", 		new[] {typeof(Packet)})},
			{'r', Tuple.Create<int, bool, Action, string, Type[]>(5, true,  DumpRegexMatches, 	 "regex matches", 	new[] {typeof(ConsoleCmd)})},
			{'R', Tuple.Create<int, bool, Action, string, Type[]>(8, false, SearchRecursively, 	 null,				new Type[0])},
			{'j', Tuple.Create<int, bool, Action, string, Type[]>(5, false, DumpJumps, 			 "jump list", 		new[] {typeof(ConsoleCmd)})}
		};

		// this is a separate thing cuz the above list is already supa fat
		private static readonly Dictionary<string, string> HelpMessages = new Dictionary<string, string> {
			{"-f", $" <filename>   (def.=\"{DefaultFolderName}\") Output other options to separate files"},
			{"-s", $"              Sort demos alphanumerically before doing other options"},
			{"-l", $"              Print demo info in a format similar to listdemo++"},
			{"-v", $"              Dump all parsable information about the demo"},
			{"-m", $"              Dump the bytes of all svc/net messages in hex"},
			{"-r", $" <regex>      (def.=\"{DefaultRegex}\") Dumps all console commands where the regex matches"},
			{"-p", $"              Dump every |tick|~|x,y,z|pitch,yaw,roll|~{{player2}}~... for all players"},
			{"-c", $"              Dump all suspicious commands and cheats"},
			{"-R", $"              Search folders recursively"},
			{"-j", $"              Dump all jumps found in the demo"}
		};

		// pre-parsing/const stuff
		private static readonly List<string> UserDirs = new List<string>();
		private static readonly List<String> UserOptions = new List<string>(); // options that aren't "-h"
		private static Type[] _packetTypesToParse;
		private static String UsageStr => "Usage: \"" + AppDomain.CurrentDomain.FriendlyName + "\" [demos/dirs] <options>";
		private const String DefaultFolderName = "parser output";
		private const String DefaultRegex = ".*";
		private static bool _recursive = false;


		public static void ParseOptions(string[] args) {
			if (args.Length == 1) {
				if (args[0] == "-h" || args[0] == "--help" || args[0] == "/?") {
					PrintFullHelp();
					Environment.Exit(0);
				}
				if (args[0] == "--version") {
					Console.WriteLine($"v{Version}");
					Environment.Exit(0);
				}
			}

			bool helpOptionSet = false; // handled separately
			bool expectingDirs = true;
			for (int i = 0; i < args.Length; i++) {
				string arg = args[i];
				if (expectingDirs) {
					// if the current arg is an option, redo the loop but w/ expectingOptions == true
					if (arg.Length == 2 && arg[0] == '-') {
						expectingDirs = false;
						i--;
						continue;
					}

					// assume the arg is a demo/dir
					try {
						DirectoryInfo dirInfo = new DirectoryInfo(arg);
						if (!File.Exists(dirInfo.FullName) && !Directory.Exists(dirInfo.FullName)) {
							Console.WriteLine($"{arg} does not exist");
							Environment.Exit(1);
						}

						// skip over files that aren't demo files
						if ((dirInfo.Attributes & FileAttributes.Directory) == 0 && dirInfo.Extension != ".dem")
							continue;
						UserDirs.Add(arg);
					}
					catch (ArgumentException) {
						Console.WriteLine($"{arg} isn't a valid readable file/folder");
						Environment.Exit(-1);
					}
				} else {
					if (args[i] == "-h") {
						helpOptionSet = true;
						continue;
					}

					// check to make sure option matches '-{char}' & that the option exists
					if (arg.Length != 2 || arg[0] != '-' || !OptionList.Keys.Contains(arg[1])) {
						Console.WriteLine($"unrecognized option: {arg}");
						Environment.Exit(1);
					}

					// check for duplicate options
					if (UserOptions.Select(uOption => uOption.Substring(0, 2)).Contains(arg.Substring(0, 2))) {
						Console.WriteLine($"{arg.Substring(0, 2)} given twice");
						Environment.Exit(1);
					}

					UserOptions.Add(arg);
					// if the option allows a string after it & there are args remaining
					if (OptionList[arg[1]].Item2 && i < args.Length - 1) {
						// if the next option doesn't match '-{char}' where {char} is an existing option, assume it's the string that follows the option
						if (args[i + 1].Length != 2 || args[i + 1][0] != '-' ||
							!OptionList.ContainsKey(args[i + 1][1])) {
							UserOptions[UserOptions.Count - 1] += " " + args[i + 1];
							i++; // skip over the string that comes after this option
						}
					}
				}
			}

			if (helpOptionSet) {
				if (UserOptions.Count == 0)
					PrintFullHelp();
				else
					PrintOptionHelp();
				Environment.Exit(0);
			}

			if (UserOptions.Count == 0 && UserDirs.Count == 0) {
				PrintShortUsageString();
				Console.ReadKey();
				Environment.Exit(0);
			}
			
			// sorts the option properties based on priority
			List<Tuple<int, bool, Action, string, Type[]>> optionProperties = UserOptions.Select(uOption => OptionList[uOption[1]])
				.OrderByDescending(tuple => tuple.Item1).ToList();
			
			// sets the packets that are required to be parsed based on the user options
			_packetTypesToParse = optionProperties.SelectMany(tuple => tuple.Item5).ToArray();
			
			_normalOptions = optionProperties.Where(tuple => tuple.Item1 <= 5).ToList();
			if (_normalOptions.Count == 0 && optionProperties.Count > 0) {
				Console.WriteLine("No options were given to do anything");
				Environment.Exit(0);
			}

			// perform all options who's priority is greater than that of sorting
			optionProperties.Where(tuple => tuple.Item1 > OptionList['s'].Item1).ToList().ForEach(tuple => tuple.Item3());
			
			Console.Write("Finding files... ");
			
			// add all the demos to the demo path list
			UserDirs.ForEach((string userDir) => {
				string fullDirName = new DirectoryInfo(userDir).FullName;
				if (File.Exists(fullDirName))
					_demoPaths.Add(fullDirName);
				else // is folder
					_demoPaths.AddRange(Directory.GetFiles(userDir, "*.dem", 
						_recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));
			});
			Console.WriteLine($"found {_demoPaths.Count}");
			
			// perform all options who's priority is above 5 (normal options) and less than or equal to that of sorting
			optionProperties.Where(tuple => tuple.Item1 > 5 && tuple.Item1 <= OptionList['s'].Item1).ToList().ForEach(tuple => tuple.Item3());
			MainLoop();
		}


		private static void PrintShortUsageString() {
			Console.WriteLine($"\n{UsageStr}");
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine("Use -h, --help, or /? to get full help\n");
			Console.ResetColor();
		}


		private static void PrintFullHelp() {
			Console.WriteLine($"\n{UsageStr}");
			Console.WriteLine($"v{Version}");
			Console.WriteLine("Prints information in demo files & provides some parsing functionality.\n" +
							  "With no arguments prints information similar to listdemo.\n");
			Console.WriteLine("Getting help:\n" +
							  "-h  print all options' usage; if other options exist, prints only their usage");
			Console.WriteLine("Options:");
			foreach (KeyValuePair<string,string> option in HelpMessages)
				Console.WriteLine($"{option.Key}{option.Value}");
			Console.WriteLine("\nExamples:");
			Console.WriteLine("cheater.dem -r \"(^| |;|\\\")sv_cheats -?(\\.[1-9]+|0*[1-9])(\\\"|;| |$))\" -p\nPrints any activation of sv_cheats followed by the position information.\n");
			Console.WriteLine("fat.dem fatter.dem -v -f bigJuice -l\nDumps verbose & listdemo output to separate files in a folder called bigJuice.\n");
			Console.WriteLine("demodir -f -m -h -c\nPrints help for the -f, -m, & -c options.\n");
		}


		private static void PrintOptionHelp() {
			foreach (string option in UserOptions.Select(option => option.Substring(0, 2))) {
				Console.WriteLine(HelpMessages.ContainsKey(option)
					? $"{option}{HelpMessages[option]}"
					: $"No help description written for {option}");
			}
		}


		private static void SetOutputToFile() {
			_writeToFile = true;
			// get the full option: -f folderName
			string fullOption = UserOptions.Single(uOption => uOption[1] == GetOptionFromFunc().Key);
			if (fullOption.Length == 2) {// no folder name specified
				_folderName = DefaultFolderName;
			} else {
				try {
					_folderName = fullOption.Substring(fullOption.IndexOf(' ') + 1); // get th
					_folderName = Path.GetFileName(_folderName);
				}
				catch (Exception e) when (e is PathTooLongException || e is ArgumentException) {
					Console.WriteLine($"{_folderName} is not a valid folder name");
					Environment.Exit(0);
				}
			}
		}


		private static void SearchRecursively() {
			_recursive = true;
		}


		private static void SortDemos() {
			_demoPaths = _demoPaths.OrderBy(s => new DirectoryInfo(s).Name).ToList();
		}
	}
}