using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DemoParser.Parser;
using DemoParser.Parser.Components;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.Components.Messages;
using DemoParser.Parser.Components.Messages.UserMessages;
using DemoParser.Parser.Components.Packets;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace ConsoleApp {
	
	public static partial class ConsoleFunctions {

		private static string _folderPath;
		private static TextWriter _curTextWriter;
		private static BinaryWriter _curBinWriter;
		private static readonly List<SourceDemo> Demos = new List<SourceDemo>();
		private static SourceDemo CurDemo => Demos[^1];


		private static void SetTextWriter(string suffix) {
			if (_folderPath != null) {
				_curTextWriter?.Flush(); // i can't flush this after it's been closed
				_curTextWriter?.Close();
				_curTextWriter = new StreamWriter(Path.Combine(_folderPath, $"{CurDemo.FileName[..^4]} - {suffix}.txt"));
			} else {
				_curTextWriter = Console.Out;
			}
		}


		// if changing how the output is named, change the tests as well
		private static void SetBinaryWriter(string suffix, string fileFormat) {
			if (_folderPath == null)
				throw new InvalidOperationException($"The option for '{suffix}' requires the folder option to be set.");
			_curBinWriter?.Flush();
			_curBinWriter?.Close();
			_curBinWriter = new BinaryWriter(new FileStream(
				Path.Combine(_folderPath, $"{CurDemo.FileName[..^4]}-{suffix}.{fileFormat}"), FileMode.Create));
		}


		private static void ConsFunc_VerboseDump() {
			SetTextWriter("verbose dump");
			Console.WriteLine("Dumping verbose output...");
			CurDemo.WriteVerboseString(_curTextWriter);
		}


		// this is basically copied from the old parser, i'll think about improving this
		private static void ConsFunc_ListDemo(ListdemoOption listdemoOption, bool printWriteMsg) {
			SetTextWriter("listdemo++");
			if (printWriteMsg)
				Console.WriteLine("Writing listdemo+ output...");
			ConsoleColor originalColor = Console.ForegroundColor;
			if (!CurDemo.FilterForPacket<Packet>().Any()) {
				Console.WriteLine("this demo is janked yo, it doesn't have any packets");
				return;
			}

			Console.ForegroundColor = ConsoleColor.Gray;
			if (listdemoOption == ListdemoOption.DisplayHeader) {
				DemoHeader h = CurDemo.Header;
				_curTextWriter.Write(
						$"{"Demo protocol",      -25}: {h.DemoProtocol}"    +
						$"\n{"Network protocol", -25}: {h.NetworkProtocol}" +
						$"\n{"Server name",      -25}: {h.ServerName}"      +
						$"\n{"Client name",      -25}: {h.ClientName}"      +
						$"\n{"Map name",         -25}: {h.MapName}"         +
						$"\n{"Game directory",   -25}: {h.GameDirectory}"   +
						$"\n{"Playback time",    -25}: {h.PlaybackTime:F3}" +
						$"\n{"Ticks",            -25}: {h.TickCount}"       +
						$"\n{"Frames",           -25}: {h.FrameCount}"      +
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
				foreach (ConsoleCmd cmd in CurDemo.FilterForRegexMatches(regexes[i]).DistinctBy(cmd => cmd.Tick)) {
					Console.ForegroundColor = colors[i];
					_curTextWriter.WriteLine($"{names[i]} detected on tick {cmd.Tick}, " +
											 $"time {cmd.Tick * CurDemo.DemoSettings.TickInterval:F3}");
				}
			}
			// matches any flag like 'echo #some_flag_name#`
			Regex flagMatcher = new Regex(@"^\s*echo\s+#(?<flag_name>\S*?)#\s*$", RegexOptions.IgnoreCase);
 			
			// gets all flags, then groups them by the flag type, and sorts the ticks of each type
			// in this case, #flag# is treated the same as #FLAG#
			var flagGroups = CurDemo.FilterForRegexMatches(flagMatcher)
				.Select(cmd => (
						Matches: flagMatcher.Matches(cmd.Command)[0].Groups["flag_name"].Value.ToUpper(), 
						cmd.Tick)
					)
				.GroupBy(tuple => tuple.Matches)
				.ToDictionary(tuples => tuples.Key,
					tuples => 
						tuples
						.Select(tuple => tuple.Tick)
						.Distinct()
						.OrderBy(i => i)
						.ToList()
						)
				.Select(pair => Tuple.Create(pair.Key, pair.Value));
 
			foreach ((string flagName, List<int> ticks) in flagGroups) {
				
				Console.ForegroundColor = Console.ForegroundColor == ConsoleColor.Yellow 
					? ConsoleColor.Magenta 
					: ConsoleColor.Yellow;
				
				foreach (int i in ticks) {
					_curTextWriter.WriteLine($"'{flagName}' flag detected on tick {i}, " +
											 $"time {i * CurDemo.DemoSettings.TickInterval:F3}");
				}
			}
 
			Console.ForegroundColor = ConsoleColor.Cyan;
			_curTextWriter.WriteLine(
				$"\n{"Measured time: ",  -25}: {(CurDemo.TickCount() - 1) * CurDemo.DemoSettings.TickInterval:F3}" +
				$"\n{"Measured ticks: ", -25}: {(CurDemo.TickCount() - 1)}\n");
			Console.ForegroundColor = originalColor;
		}


		private static void ConsFunc_RegexSearch(string pattern) {
			SetTextWriter("regex matches");
			Console.Write("Finding regex matches...");
			List<ConsoleCmd> matches = CurDemo.FilterForRegexMatches(new Regex(pattern)).ToList();
			if (matches.Count == 0) {
				const string s = "no matches found";
				Console.WriteLine($" {s}");
				if (_curTextWriter != Console.Out)
					_curTextWriter.WriteLine(s);
			} else {
				Console.WriteLine();
				matches.ForEach(cmd => {_curTextWriter.WriteLine($"[{cmd.Tick}] {cmd.Command}");});
			}
		}


		private static void ConsFunc_DumpJumps() {
			SetTextWriter("jump dump");
			Console.Write("Finding jumps...");
			List<ConsoleCmd> matches = CurDemo.FilterForRegexMatches(new Regex("[-+]jump")).ToList();
			if (matches.Count == 0) {
				const string s = "no jumps found";
				Console.WriteLine($" {s}");
				if (_curTextWriter != Console.Out)
					_curTextWriter.WriteLine(s);
			} else {
				Console.WriteLine();
				matches.ForEach(cmd => {_curTextWriter.WriteLine($"[{cmd.Tick}] {cmd.Command}");});
			}
		}


		private static void ConsFunc_DumpCheats() {
			SetTextWriter("cheat dump");
			Console.WriteLine("Finding cheats...");
			List<ConsoleCmd> matches = CurDemo.FilterForRegexMatches(new Regex(new[] {
				"host_timescale", "god", "sv_cheats", "buddha", "host_framerate", "sv_accelerate", "gravity", 
				"sv_airaccelerate", "noclip", "impulse", "ent_", "sv_gravity", "upgrade_portalgun", 
				"phys_timescale", "notarget", "give", "fire_energy_ball", "_spt_", "plugin_load", 
				"ent_parent", "!picker", "ch_"
			}.SequenceToString(")|(", "(", ")"))).ToList();
			if (matches.Count == 0) {
				const string s = "no cheats found";
				Console.WriteLine($" {s}");
				if (_curTextWriter != Console.Out)
					_curTextWriter.WriteLine(s);
			} else {
				Console.WriteLine();
				matches.ForEach(cmd => {_curTextWriter.WriteLine($"[{cmd.Tick}] {cmd.Command}");});
			}
		}


		private static void ConsFunc_RemoveCaptions() {
			SetBinaryWriter("captions_removed", "dem");
			Console.WriteLine("Removing captions...");
			
			var closeCaptionPackets = CurDemo.FilterForPacket<Packet>()
				.Where(packet => packet.FilterForMessage<SvcUserMessageFrame>()
					.Any(frame => frame.UserMessageType == UserMessageType.CloseCaption)).ToArray();
			
			int changedPackets = 0;
			_curBinWriter.Write(CurDemo.Header.Reader.ReadRemainingBits().bytes);
			
			foreach (PacketFrame frame in CurDemo.Frames) {
				if (frame.Packet != closeCaptionPackets[changedPackets]) {
					_curBinWriter.Write(frame.Reader.ReadRemainingBits().bytes); // write frames that aren't changed
				} else {
					Packet p = (Packet)frame.Packet;
					BitStreamWriter bsw = new BitStreamWriter(frame.Reader.ByteLength);
					var last = p.MessageStream.Last().message;
					int len = last.Reader.Start - frame.Reader.Start + last.Reader.BitLength;
					bsw.WriteBits(frame.Reader.ReadBits(len), len);
					int msgSizeOffset = p.MessageStream.Reader.Start - frame.Reader.Start;
					int typeInfoLen = CurDemo.DemoSettings.NetMsgTypeBits + CurDemo.DemoSettings.UserMessageLengthBits + 8;
					bsw.RemoveBitsAtIndices(p.FilterForUserMessage<CloseCaption>()
						.Select(caption => (caption.Reader.Start - frame.Reader.Start - typeInfoLen, 
							caption.Reader.BitLength + typeInfoLen)));
					bsw.WriteUntilByteBoundary();
					bsw.EditIntAtIndex((bsw.BitLength - msgSizeOffset - 32) >> 3, msgSizeOffset, 32);
					_curBinWriter.Write(bsw.AsArray);
					
					// if we've edited all the packets, write the rest of the data in the demo
					if (++changedPackets == closeCaptionPackets.Length) {
						BitStreamReader tmp = CurDemo.Reader;
						tmp.SkipBits(frame.Reader.Start + frame.Reader.BitLength);
						_curBinWriter.Write(tmp.ReadRemainingBits().bytes);
						break;
					}
				}
			}
		}


		private static void ConsFunc_DumpDataTables() {
			SetTextWriter("data tables dump");
			Console.WriteLine("Dumping data tables...");
			foreach ((DataTables tables, int j) in CurDemo.FilterForPacket<DataTables>().Select((tables, i) => (tables, i))) {
				if (j > 0)
					_curTextWriter.Write("\n\n\n");
				_curTextWriter.WriteLine(tables.ToString());
				_curTextWriter.Write("\n\nClass hierarchy:\n\n");
				_curTextWriter.Write(new DataTableTree(tables, true));
			}
		}


		private static void ConsFunc_DumpActions(ActPressedDispType opt) {
			SetTextWriter("buttons pressed");
			Console.WriteLine("Getting buttons pressed...");
			foreach (UserCmd userCmd in CurDemo.FilterForPacket<UserCmd>()) {
				_curTextWriter.Write($"[{userCmd.Tick}] ");
				if (!userCmd.Buttons.HasValue) {
					_curTextWriter.WriteLine("null");
					continue;
				}
				Buttons b = userCmd.Buttons.Value;
				switch (opt) {
					case ActPressedDispType.AsTextFlags:
						_curTextWriter.WriteLine(b.ToString());
						break;
					case ActPressedDispType.AsInt:
						_curTextWriter.WriteLine((uint)b);
						break;
					case ActPressedDispType.AsBinaryFlags:
						_curTextWriter.WriteLine(Convert.ToString((uint)b, 2).PadLeft(32, '0'));
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(opt), opt, "unsupported buttons pressed option");
				}
			}
		}


		private static void ConsFunc_LinkDemos() { // todo when this sets the writer, give it a custom name
			Console.WriteLine("Link demos feature not implemented yet.");
		}
	}
}