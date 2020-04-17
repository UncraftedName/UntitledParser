using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DemoParser.Parser;
using DemoParser.Utils;

namespace ConsoleApp {
	
	public static partial class ConsoleFunctions {
		
		private static string UsageStr => $"Usage: \"{AppDomain.CurrentDomain.FriendlyName}\" <demos/dirs> [options]";
		
		// todo add prop table dump
		// todo add prop tracker
		// todo add buttons pressed
		public static void Main(string[] args) {
			
			Console.WriteLine("DemoParser by UncraftedName");
			
			switch (args.Length) {
				case 0:
					PrintShortUsageString();
					Console.Read();
					Environment.Exit(0);
					break;
				case 1 when args[0].EndsWith(".dem") && File.Exists(args[0]): // just behave like listdemo+
					Demos.Add(new SourceDemo(args[0]));
					try {
						CurrentDemo.Parse();
						ListDemo(default);
					} catch (Exception e) {
						Console.WriteLine($"there was a problem while parsing... {e.Message}");
						Console.Read();
						Environment.Exit(1);
					}
					Console.Read();
					Environment.Exit(0);
					break;
			}

			// directory info should work here, will probably change to something better in the future™
			var pathsArg = new Argument<DirOrPath[]>("paths") {
				Description = "demo paths or folders",
				Arity = ArgumentArity.OneOrMore
			};
			pathsArg.AddValidator(symbol => {
				string s = symbol.Tokens[0].Value;
				Console.WriteLine(Path.GetFullPath(s));
				bool d = Directory.Exists(s);
				bool f = File.Exists(s);
				if (!d && !f)
					return $"path \"{s}\" does not exist";
				if (f && !s.EndsWith(".dem"))
					return $"\"{s}\" is not a demo file";
				return null;
			});
			
			
			var regexOpt = new Option(new[] {"-R", "--regex"},
				"search for console commands by regex (.* will display everything)") {
				Required = false,
				Argument = new Argument<string>("regex") {
					Arity = ArgumentArity.ExactlyOne,
					Description = "regex used to find console commands",
				}
			};
			regexOpt.AddValidator(symbol => CheckIfValidRegex(symbol.Tokens[0].Value));
			
			
			var verboseOpt = new Option(new [] {"-v", "--verbose"},
				"dump all parsable info in the demo to a text file (needs -f)") {
				Required = false
			};
			
			
			var folderOpt = new Option(new [] {"-f", "--folder"},
				"specify a folder to dump the parser output to") {
				Required = false,
				Argument = new Argument<DirectoryInfo>("folder") {
					Arity = ArgumentArity.ExactlyOne,
					Description = "the folder to dump the parser output to"
				}
			};
			folderOpt.AddValidator(symbol => CheckIfValidFolderName(symbol.Tokens[0].Value));
			

			// probs should figure out how to use separate commands, since i want to determine when the option is triggered
			// and what the argument is separately
			var listDemoOpt = new Option(new [] {"-l", "--listdemo"},
				"print demo info in a format similar to listdemo+") {
				Required = false,
				Argument = new Argument<ListdemoOption>("listdemoOption", default) {
					Arity = ArgumentArity.ZeroOrOne,
					Description = "determines whether or not to display demo header info"
				}
			};
			

			var cheatOpt = new Option(new [] {"-c", "--cheats"},
				"shows any cheats detected in the demo") {
				Required = false
			};
			

			var jumpOpt = new Option(new [] {"-j", "--jumps"},
				"shows any detection of jumps") {
				Required = false
			};
			
			var recursiveOpt = new Option(new [] {"-r", "--recursive"},
				"specifies whether the search for demos should be recursive") {
				Required = false
			};
			
			
			var linkOption = new Option(new [] {"--link"},
				"attempt to link demos together to check if they are part of a continuous run (not implemented)") {
					Required = false
			};
			linkOption.AddValidator(_ => "link option not implemented yet");


			var rootCommand = new RootCommand {
				pathsArg,
				regexOpt,
				verboseOpt,
				folderOpt,
				listDemoOpt,
				cheatOpt,
				jumpOpt,
				recursiveOpt
			};
			
			if (Debugger.IsAttached) // link option not implemented yet
				rootCommand.AddOption(linkOption);
			
			
			rootCommand.AddValidator(res => {
				if (!res.Children.Any(
					result => result.GetType() != typeof(ArgumentResult) 			// don't care about arguments
							  && result.Name != folderOpt.Name 						// don't care about --folder option
							  && result.Name != recursiveOpt.Name					// don't care about --recursive option
							  && !((result as OptionResult)?.IsImplicit ?? true))) 	// don't care about implicit options (like listdemo can be)
				{
					return "no runnable options given"; // if there are no options left after those ^ conditions, then none have been provided
				}
				
				if (res[verboseOpt.Aliases[0]] != null && res[folderOpt.Aliases[0]] == null)
					return "verbose option requires -f";
				return null;
			});


			void CommandAction(
				DirOrPath[] paths, 
				string regex, 
				bool verbose, 
				DirectoryInfo folder, 
				ListdemoOption listdemo, 
				bool cheats, 
				bool recursive, 
				bool link,
				bool jumps,
				ParseResult parseResult) // this is part of System.CommandLine - it will automatically put the result in here
			{
				// add paths to set to make sure i don't parse the same demo several times
				HashSet<FileInfo> demoPaths = new HashSet<FileInfo>();
				foreach (DirOrPath dirOrPath in paths) {
					if (dirOrPath.IsDir) {
						demoPaths.UnionWith(dirOrPath.DirectoryInfo.EnumerateFiles("*.dem", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));
					} else {
						if (!dirOrPath.Name.EndsWith(".dem"))
							PrintErrorAndExit($"{dirOrPath} is not a demo file");
						else
							demoPaths.Add(dirOrPath.FileInfo);
					}
				}
				
				if (!demoPaths.Any())
					PrintErrorAndExit("No demos to parse...");
				
				if (link && demoPaths.Count < 2)
					PrintErrorAndExit("The link option should have at least 2 demos");
				
				if (folder != null) {
					folder.Create();
					_folderPath = folder.FullName;
				}

				List<FileInfo> orderedPaths = demoPaths.OrderBy(s => s.Name).ToList();
				// these are sorted, so any similarities between the first and last path must be shared between all paths
				string commonParentPath = orderedPaths.Count == 1 
					? orderedPaths[0].DirectoryName 
					: ParserTextUtils.SharedSubstring(orderedPaths[0].FullName, orderedPaths[^1].FullName);
				
				// main loop
				foreach (FileInfo demoPath in orderedPaths) {
					
					// if the common path is empty, that means the demos span multiple drives, so just use the full name
					string displayPath = commonParentPath == "" 
						? demoPath.Name 
						: Path.GetRelativePath(commonParentPath, demoPath.FullName);
					
					try {
						Console.Write($"Parsing \"{displayPath}\"... ");
						
						using (var progressBar = new ProgressBar()) {
							// if i'm not linking demos there's no point in keeping all of them
							if (!link)
								Demos.Clear();
							Demos.Add(new SourceDemo(demoPath, progressBar));
							CurrentDemo.Parse();
						}
						Console.WriteLine("done.");
						// run all the standard options
						if (parseResult.Tokens.Any(token => listDemoOpt.HasAlias(token.Value))) {
							Console.WriteLine("Writing listdemo output...");
							ListDemo(listdemo);
						}
						if (regex != null)
							RegexSearch(regex);
						if (cheats)
							Cheats();
						if (jumps)
							Jumps();
					} catch (Exception e) {
						Console.WriteLine("failed.");
						Console.WriteLine($"Message: {e.Message}");
					}
					if (verbose && Demos.Count > 0)
						VerboseOutput(); // verbose output can still recover from an exception
				}
				if (link)
					LinkDemos();
				
				_currentWriter?.Flush();
				_currentWriter?.Dispose();
			}


			rootCommand.Handler = CommandHandler.Create((Action<DirOrPath[],string,bool,DirectoryInfo,ListdemoOption,bool,bool,bool,bool,ParseResult>)CommandAction);

			
			rootCommand.Invoke(args);
		}
		
		
		private static void PrintShortUsageString() {
			Console.WriteLine($"\n{UsageStr}");
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine("Use -h, --help, or /? to get full help\n");
			Console.ResetColor();
		}
		
		
		private static string CheckIfValidRegex(string pattern) {
			try {
				new Regex(pattern);
			} catch (Exception e) {
				return e.Message;
			}
			return null;
		}


		private static string CheckIfValidFolderName(string name) {
			try {
				Path.GetFullPath(name);
				// i don't understand, this ^ doesn't work :/
				if (Path.GetInvalidPathChars().AsEnumerable().ToHashSet().Intersect(name.ToCharArray().AsEnumerable()).Any())
					return "path name contains invalid characters";
				if (name.Length > 240)
					return "path name too long";
			} catch (Exception e) {
				return e.Message;
			}
			return null;
		}


		private static void PrintErrorAndExit(string s) {
			ConsoleColor c = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine(s);
			Console.ForegroundColor = c;
			Console.Out.Flush();
			Environment.Exit(1);
		}
	}


	public enum ListdemoOption {
		DisplayHeader,
		SuppressHeader
	}
}