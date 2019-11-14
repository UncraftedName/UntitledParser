using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using UncraftedDemoParser.Parser;
using UncraftedDemoParser.Parser.Components.Packets;
using UncraftedDemoParser.Parser.Misc;
using UncraftedDemoParser.Utils;

namespace UncraftedDemoParser.ConsoleParsing {
	
	internal static partial class ConsoleOptions {


		// fields used during the main loop of parsing
		private static List<string> _demoPaths = new List<string>();
		private static string _folderName = DefaultFolderName;
		private static List<Tuple<int, bool, Action, string, Type[]>> _normalOptions = new List<Tuple<int, bool, Action, string, Type[]>>();
		private static bool _writeToFile = false;
		private static SourceDemo _currentDemo;
		// used when -f is set; maps the option function name to a writer
		private static Dictionary<string, TextWriter> _writers;


		// run after the setup is done, loops over every demo and displays/writes each option
		private static void MainLoop() {
			if (_demoPaths.Count == 0) {
				Environment.Exit(0);
			}
			Console.ForegroundColor = ConsoleColor.White;
			Console.WriteLine($"listdemo++ v{Version} by UncraftedName\n");
			Console.ForegroundColor = ConsoleColor.Gray;
			bool noUserArgs = _normalOptions.Count == 0;
			// if called without args (besides files), add the list demo output to the option list
			if (_normalOptions.Count == 0) {
				_normalOptions.Add(OptionList.Values.Single(tuple => tuple.Item3 == (Action)PrintListDemoOutput));
				_packetTypesToParse = new[] {typeof(ConsoleCmd)};
			}

			if (_writeToFile)
				Directory.CreateDirectory(_folderName);
			// only used if -f is set; maps the demo name to how many times that demo name has appeared
			Dictionary<string, int> outputFileCounter = new Dictionary<string, int>();
			foreach (string demoPath in _demoPaths) {
				_currentDemo = new SourceDemo(demoPath, parse: false);
				string relativeDir = demoPath.GetPathRelativeTo(Application.ExecutablePath);
				if (relativeDir.Length >= 9 && relativeDir.Substring(0, 9) == "../../../")
					relativeDir = demoPath;
				Console.WriteLine($"Parsing {relativeDir}...");
				Debug.WriteLine($"Parsing {Path.GetFullPath(demoPath)}...");
				try {
					_currentDemo.ParseHeader();
					if (_currentDemo.DemoSettings.Game != SourceDemoSettings.SourceGame.UNKNOWN)
						Console.WriteLine($"game detected - {_currentDemo.DemoSettings.Game}");
					_currentDemo.QuickParse();
					_currentDemo.ParseForPacketTypes(_packetTypesToParse);
				} catch (FailedToParseException e) {
					Debug.WriteLine(e.ToString());
					Console.WriteLine($"{e.Message}... skipping demo.");
					continue;
				} catch (Exception e) {
					Debug.WriteLine(e.StackTrace);
					Console.WriteLine($"Exception thrown from {new StackTrace(e).GetFrame(0).GetMethod().Name}");
					Console.WriteLine("Failed to parse demo. Like, super failed... skipping demo.");
					continue;
				}
				if (_writeToFile) {
					_writers = new Dictionary<string, TextWriter>(); // create a new writer for every option and every demo
					foreach (var optionProperties in _normalOptions) {
						string outputFileName = $"{_folderName}{Path.DirectorySeparatorChar}{Path.GetFileNameWithoutExtension(_currentDemo.Name)} -- {optionProperties.Item4}";
						if (outputFileCounter.ContainsKey(outputFileName)) { // checks if a demo with the same name has already been parsed, if so adds {num} to it
							outputFileCounter[outputFileName] = outputFileCounter[outputFileName] + 1; // not sure if ++ can be used here
							outputFileName = outputFileName.Insert(outputFileName.Length - optionProperties.Item4.Length - 3, // -3 for the " -- " 
								$"{{{outputFileCounter[outputFileName]}}} ");
						} else {
							outputFileCounter.Add(outputFileName, 1);
						}
						outputFileName += ".txt";
						_writers.Add(optionProperties.Item3.Method.Name, new StreamWriter(new FileStream(outputFileName, FileMode.Create)));
					}
				}
				foreach (var normalOption in _normalOptions) {
					if (!_writeToFile)
						Console.WriteLine($"{normalOption.Item4}:");
					normalOption.Item3();
				}
				if (_writeToFile)
					foreach (TextWriter writer in _writers.Values)
						writer.Dispose();
			}

			if (noUserArgs) {
				Console.ReadKey();
				Environment.Exit(0);
			}
		}


		// if writing to file, gets the right writer by looking up the writer by the name of the caller method
		private static void WriteToOutput(string str, [CallerMemberName] string caller = "") {
			if (_writeToFile)
				_writers[caller].Write(str);
			else
				Console.Write(str);
		}


		private static void WriteLineToOutput(string str = "", [CallerMemberName] string caller = "") {
			WriteToOutput(str, caller);
			WriteToOutput("\n", caller);
		}


		// used to reverse lookup the option char, so that the chars don't have to be hard coded
		private static KeyValuePair<char, Tuple<int, bool, Action, string, Type[]>> GetOptionFromFunc([CallerMemberName] string caller = "") {
			return OptionList.Single(pair => pair.Value.Item3.Method.Name == caller);
		}


		private static void PrintListDemoOutput() {
			try {
				if (_writeToFile)
					_currentDemo.PrintListdemoOutput(true, false, _writers[MethodBase.GetCurrentMethod().Name]);
				else
					_currentDemo.PrintListdemoOutput(true, false);
			} catch (FailedToParseException e) {
				Debug.WriteLine(e.StackTrace);
				Console.WriteLine(e.Message);
			}
		}


		private static void PrintVerboseOutput() {
			WriteLineToOutput(_currentDemo.AsVerboseString());
		}


		private static void DumpSvcMessages() {
			WriteLineToOutput(_currentDemo.SvcMessagesCounterAsString());
			WriteLineToOutput(_currentDemo.SvcMessagesAsString());
		}


		// |tick|-|x1,y1,z1|p1,y1,r1|-|x2,y2,z2|p2,y2,r2|
		private static void DumpPositions() {
			List<Packet> packets = _currentDemo.FilteredForPacketType<Packet>();
			if (packets.Count == 0)
				WriteLineToOutput("no packets found, you have a very strange demo file");
			else 
				packets.ForEach(packet => {
					WriteToOutput($"|{packet.Tick}|");
					WriteLineToOutput(
						packet.PacketInfo.Select(
								info => $"|{info.ViewOrigin[0]},{info.ViewOrigin[1]},{info.ViewOrigin[2]}|" +
										$"{info.ViewAngles[0]},{info.ViewAngles[1]},{info.ViewAngles[2]}|")
							.QuickToString("~", "~", ""));
				});
		}


		private static void DumpRegexMatches() {
			string userRegexOption = UserOptions.Find(s => s[1] == GetOptionFromFunc().Key);
			var matches = _currentDemo.PacketsWhereRegexMatches(
				new Regex(userRegexOption.Length == 2 ? DefaultRegex : userRegexOption.Substring(userRegexOption.IndexOf(' ') + 1)));
			if (matches.Count == 0)
				WriteLineToOutput("no matches found");
			else
				matches.ForEach(cmd => WriteLineToOutput($"[{cmd.Tick}] {cmd.Command}"));
		}


		private static void DumpCheats() {
			List<ConsoleCmd> matches = _currentDemo.PacketsWhereRegexMatches(new Regex(new[] {
				"host_timescale", "god", "sv_cheats", "buddha", "host_framerate", "sv_accelerate", "gravity", 
				"sv_airaccelerate", "noclip", "impulse", "ent", "sv_gravity", "upgrade_portalgun", 
				"phys_timescale", "notarget", "give", "fire_energy_ball", "spt", "plugin_load", 
				"ent_parent", "!picker", ";"
			}.QuickToString(")|(", "(", ")")));
			if (matches.Count == 0)
				WriteLineToOutput("no suspicious commands found");
			else
				matches.ForEach(cmd => WriteLineToOutput($"[{cmd.Tick}] {cmd.Command}"));
		}


		private static void DumpJumps() {
			List<ConsoleCmd> matches = _currentDemo.PacketsWhereRegexMatches(new Regex("[-+]jump"));
			if (matches.Count == 0)
				WriteLineToOutput("no jumps found");
			else
				matches.ForEach(cmd => WriteLineToOutput($"[{cmd.Tick}] {cmd.Command}"));
		}
	}
}