using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using DemoParser.Parser;
using DemoParser.Parser.Components;
using DemoParser.Parser.Components.Packets;
using DemoParser.Utils;

namespace ConsoleParser {
	
	public static partial class ConsoleFunctions {

		private static string _folderPath;
		private static TextWriter _currentWriter;
		private static readonly List<SourceDemo> Demos = new List<SourceDemo>();
		private static SourceDemo CurrentDemo => Demos[^1];


		private static void SetWriter([CallerMemberName] string callerName = "") {
			if (_folderPath != null) {
				_currentWriter?.Flush(); // i can't flush this after it's been closed
				_currentWriter?.Close();
				_currentWriter = new StreamWriter(Path.Combine(_folderPath, $"{CurrentDemo.FileName} -- {callerName}.txt"));
			} else {
				_currentWriter = Console.Out;
			}
		}


		private static void VerboseOutput() {
			SetWriter();
			Console.WriteLine("Dumping verbose output...");
			_currentWriter.Write(CurrentDemo.ToString());
		}


		// this is basically copied from the old parser, i'll think about improving this
		private static void ListDemo(ListdemoOption listdemoOption) {
			SetWriter();
			ConsoleColor originalColor = Console.ForegroundColor;
			if (!CurrentDemo.FilterForPacketType<Packet>().Any()) {
				Console.WriteLine("this demo is janked yo, it doesn't have any packets");
				return;
			}

			Console.ForegroundColor = ConsoleColor.Gray;
			if (listdemoOption == ListdemoOption.DisplayHeader) {
				DemoHeader h = CurrentDemo.Header;
				_currentWriter.Write(
						$"{"Demo protocol",      -25}: {h.DemoProtocol}" 	+
						$"\n{"Network protocol", -25}: {h.NetworkProtocol}" +
						$"\n{"Server name",      -25}: {h.ServerName}" 		+
						$"\n{"Client name",      -25}: {h.ClientName}" 		+
						$"\n{"Map name",         -25}: {h.MapName}" 		+
						$"\n{"Game directory",   -25}: {h.GameDirectory}" 	+
						$"\n{"Playback time",    -25}: {h.PlaybackTime:F3}" +
						$"\n{"Ticks",            -25}: {h.TickCount}" 		+
						$"\n{"Frames",           -25}: {h.FrameCount}" 		+
						$"\n{"SignOn Length",    -25}: {h.SignOnLength}\n\n");
			}
			Regex[] regexes = {
				new Regex("^autosave$"), 
				new Regex("^autosavedangerous$"), 
				new Regex("^autosavedangerousissafe$"),
				new Regex("^startneurotoxins 99999$")
			};
			// all appended by "detected on tick..."
			string[] names = {
				"Autosavedangerous command",
				"Autosavedangerousissafe command",
				"Autosave command",
				"End of game"
			};
			ConsoleColor[] colors = {
				ConsoleColor.DarkYellow,
				ConsoleColor.DarkYellow,
				ConsoleColor.DarkYellow,
				ConsoleColor.Green
			};
			for (int i = 0; i < regexes.Length; i++) {
				foreach (ConsoleCmd cmd in CurrentDemo.FilterForRegexMatches(regexes[i]).DistinctBy(cmd => cmd.Tick)) {
					Console.ForegroundColor = colors[i];
					_currentWriter.WriteLine($"{names[i]} detected on tick {cmd.Tick}, time {cmd.Tick * CurrentDemo.DemoSettings.TickInterval:F3}");
				}
			}
			// matches any flag like 'echo #some_flag_name#`
			Regex flagMatcher = new Regex(@"^\s*echo\s+#(?<flag_name>\S*?)#\s*$", RegexOptions.IgnoreCase);
 			
			// gets all flags, then groups them by the flag type, and sorts the ticks of each type
			// in this case, #flag# is treated the same as #FLAG#
			var flagGroups = CurrentDemo.FilterForRegexMatches(flagMatcher)
				.Select(cmd =>
					Tuple.Create(flagMatcher.Matches(cmd.Command)[0].Groups["flag_name"].Value.ToUpper(), cmd.Tick))
				.GroupBy(tuple => tuple.Item1)
				.ToDictionary(tuples => tuples.Key,
					tuples => tuples.Select(tuple => tuple.Item2).Distinct().OrderBy(i => i).ToList())
				.Select(pair => Tuple.Create(pair.Key, pair.Value));
 
			foreach ((string flagName, List<int> ticks) in flagGroups) {
				Console.ForegroundColor = Console.ForegroundColor == ConsoleColor.Yellow ? ConsoleColor.Magenta : ConsoleColor.Yellow;
				foreach (int i in ticks)
					_currentWriter.WriteLine($"'{flagName}' flag detected on tick {i}, time {i * CurrentDemo.DemoSettings.TickInterval:F3}");
			}
 
			Console.ForegroundColor = ConsoleColor.Cyan;
			_currentWriter.WriteLine(
				$"\n{"Measured time: ",   	-25}: {(CurrentDemo.TickCount() - 1) * CurrentDemo.DemoSettings.TickInterval:F3}" +
				$"\n{"Measured ticks: ",  	-25}: {CurrentDemo.TickCount() - 1}\n");
			Console.ForegroundColor = originalColor;
		}


		private static void RegexSearch(string pattern) {
			SetWriter();
			Console.Write("Finding regex matches...");
			List<ConsoleCmd> matches = CurrentDemo.FilterForRegexMatches(new Regex(pattern)).ToList();
			if (matches.Count == 0) {
				const string s = "no matches found";
				Console.WriteLine($" {s}");
				if (_currentWriter != Console.Out)
					_currentWriter.WriteLine(s);
			} else {
				Console.WriteLine();
				matches.ForEach(cmd => {_currentWriter.WriteLine($"[{cmd.Tick}] {cmd.Command}");});
			}
		}


		private static void Jumps() {
			SetWriter();
			Console.Write("Finding jumps...");
			List<ConsoleCmd> matches = CurrentDemo.FilterForRegexMatches(new Regex("[-+]jump")).ToList();
			if (matches.Count == 0) {
				const string s = "no jumps found";
				Console.WriteLine($" {s}");
				if (_currentWriter != Console.Out)
					_currentWriter.WriteLine(s);
			} else {
				Console.WriteLine();
				matches.ForEach(cmd => {_currentWriter.WriteLine($"[{cmd.Tick}] {cmd.Command}");});
			}
		}


		private static void Cheats() {
			SetWriter();
			Console.WriteLine("Finding cheats...");
			List<ConsoleCmd> matches = CurrentDemo.FilterForRegexMatches(new Regex(new[] {
				"host_timescale", "god", "sv_cheats", "buddha", "host_framerate", "sv_accelerate", "gravity", 
				"sv_airaccelerate", "noclip", "impulse", "ent_", "sv_gravity", "upgrade_portalgun", 
				"phys_timescale", "notarget", "give", "fire_energy_ball", "_spt_", "plugin_load", 
				"ent_parent", "!picker", "ch_"
			}.SequenceToString(")|(", "(", ")"))).ToList();
			if (matches.Count == 0) {
				const string s = "no cheats found";
				Console.WriteLine($" {s}");
				if (_currentWriter != Console.Out)
					_currentWriter.WriteLine(s);
			} else {
				Console.WriteLine();
				matches.ForEach(cmd => {_currentWriter.WriteLine($"[{cmd.Tick}] {cmd.Command}");});
			}
		}


		private static void LinkDemos() { // todo when this sets the writer, give it a custom name
			Console.WriteLine("Link demos feature not implemented yet.");
		}
	}
}