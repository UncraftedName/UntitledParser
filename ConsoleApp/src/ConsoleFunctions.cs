using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DemoParser.Parser;
using DemoParser.Parser.Components;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.Components.Messages;
using DemoParser.Parser.Components.Messages.UserMessages;
using DemoParser.Parser.Components.Packets;
using DemoParser.Parser.HelperClasses;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace ConsoleApp {
	
	public static partial class ConsoleFunctions {

		private static string _folderPath;
		private static TextWriter _curTextWriter;
		private static BinaryWriter _curBinWriter;
		// I only save all demos if I need to
		private static readonly List<SourceDemo> Demos = new List<SourceDemo>();
		private static SourceDemo CurDemo => Demos[^1];
		private static readonly List<(int ticks, int adjustedTicks)> DemoTickCounts = new List<(int, int)>();
		private static int _runnableOptionCount;
		private static bool _displayMapExcludedMsg;


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


		private static int CurDemoHeaderQuickHash() {
			DemoHeader h = CurDemo.Header;
			//return HashCode.Combine(h.GameDirectory, h.NetworkProtocol, h.DemoProtocol);
			return HashCode.Combine(h.NetworkProtocol, h.DemoProtocol);
		}

		
		private static string FormatTime(double seconds) {
			string sign = seconds < 0 ? "-" : "";
			// timespan truncates values, (makes sense since 0.5 minutes is still 0 total minutes) so I round manually
			TimeSpan t = TimeSpan.FromSeconds(Math.Abs(seconds));
			int roundedMilli = t.Milliseconds + (int)Math.Round(t.TotalMilliseconds - (long)t.TotalMilliseconds);
			if (t.Hours > 0)
				return $"{sign}{t.Hours:D1}:{t.Minutes:D2}:{t.Seconds:D2}.{roundedMilli:D3}";
			else if (t.Minutes > 0)
				return $"{sign}{t.Minutes:D1}:{t.Seconds:D2}.{roundedMilli:D3}";
			else
				return $"{sign}{t.Seconds:D1}.{roundedMilli:D3}";
		}
		
		
		#region ConsFunc
		

		private static void ConsFunc_VerboseDump() {
			SetTextWriter("verbose dump");
			Console.WriteLine("Dumping verbose output...");
			CurDemo.WriteVerboseString(_curTextWriter);
		}


		// this is basically copied from the old parser, i'll think about improving this
		private static void ConsFunc_ListDemo(ListdemoOption listdemoOption, bool printWriteMsg) {
			SetTextWriter("listdemo+");
			if (printWriteMsg && _runnableOptionCount > 1)
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
						$"{"Demo protocol",      -25}: {h.DemoProtocol}"             +
						$"\n{"Network protocol", -25}: {h.NetworkProtocol}"          +
						$"\n{"Server name",      -25}: {h.ServerName}"               +
						$"\n{"Client name",      -25}: {h.ClientName}"               +
						$"\n{"Map name",         -25}: {h.MapName}"                  +
						$"\n{"Game directory",   -25}: {h.GameDirectory}"            +
						$"\n{"Playback time",    -25}: {FormatTime(h.PlaybackTime)}" +
						$"\n{"Ticks",            -25}: {h.TickCount}"                +
						$"\n{"Frames",           -25}: {h.FrameCount}"               +
						$"\n{"SignOn Length",    -25}: {h.SignOnLength}\n\n");
			}
			Regex[] regexes = {
				new Regex("^autosave$"), 
				new Regex("^autosavedangerous$"),
				new Regex("^autosavedangerousissafe$")
			};
			// all appended by "detected on tick..."
			string[] names = {
				"Autosavedangerous command",
				"Autosavedangerousissafe command",
				"Autosave command"
			};
			Console.ForegroundColor = ConsoleColor.DarkYellow;
			for (int i = 0; i < regexes.Length; i++) {
				foreach (ConsoleCmd cmd in CurDemo.FilterForRegexMatches(regexes[i]).DistinctBy(cmd => cmd.Tick)) {
					_curTextWriter.WriteLine($"{names[i]} detected on tick {cmd.Tick}, " +
											 $"time {FormatTime(cmd.Tick * CurDemo.DemoSettings.TickInterval)}");
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
 
			double tickInterval = CurDemo.DemoSettings.TickInterval;
			
			foreach ((string flagName, List<int> ticks) in flagGroups) {
				
				Console.ForegroundColor = Console.ForegroundColor == ConsoleColor.Yellow 
					? ConsoleColor.Magenta 
					: ConsoleColor.Yellow;
				
				foreach (int i in ticks) {
					_curTextWriter.WriteLine(
						$"'{flagName}' flag detected on tick {i}, time {FormatTime(i * tickInterval)}");
				}
			}

			/*CurDemo.FilterForUserMessage<Rumble>()
				.Where(tuple =>
					tuple.userMessage.RumbleType == RumbleLookup.PortalgunLeft ||
					tuple.userMessage.RumbleType == RumbleLookup.PortalgunRight)
				.Select(tuple => (tuple.tick, tuple.userMessage.RumbleType))
				.ToList()
				.ForEach(tuple => {
					(int tick, RumbleLookup rumbleType) = tuple;
					ConsoleWriteWithColor(
						$"Portal fired on tick {tick,4}, time: {FormatTime(tick * tickInterval),10}\n",
						rumbleType == RumbleLookup.PortalgunenLeft ? ConsoleColor.Cyan : ConsoleColor.Red);
				});*/

			if (listdemoOption == ListdemoOption.DisplayHeader)
				Console.WriteLine();
			
			Console.ForegroundColor = ConsoleColor.Cyan;
			_curTextWriter.WriteLine(
				$"{"Measured time ",  -25}: {FormatTime((CurDemo.TickCount() - 1) * tickInterval)}" +
				$"\n{"Measured ticks ", -25}: {CurDemo.TickCount() - 1}");

			if (CurDemo.TickCount() != CurDemo.AdjustedTickCount()) {
				_curTextWriter.WriteLine(
					$"{"Adjusted time ",-25}: {FormatTime((CurDemo.AdjustedTickCount() - 1) *  tickInterval)}");
				_curTextWriter.Write($"{"Adjusted ticks ",-25}: {CurDemo.AdjustedTickCount() - 1}");
				_curTextWriter.WriteLine($" ({CurDemo.StartAdjustmentTick}-{CurDemo.EndAdjustmentTick})");
			}

			if (_displayMapExcludedMsg)
				ConsoleWriteWithColor("(This map will be excluded from the total time)\n", ConsoleColor.DarkCyan);

			Console.ForegroundColor = originalColor;
			
			if (listdemoOption == ListdemoOption.DisplayHeader)
				Console.WriteLine();
		}



		private static void ConsFunc_DisplayTotalTime() {
			ConsoleColor originalColor = Console.ForegroundColor;
			
			if (DemoTickCounts.Any(tuple => tuple.ticks == int.MinValue)) { // some exception was thrown
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("There was an issue while calculating the total time, timing may be incorrect.");
			}
			
			Console.ForegroundColor = ConsoleColor.Green;
			
			int totalTicks = DemoTickCounts
				.Where(tuple => tuple.ticks != int.MinValue)
				.Select(tuple => tuple.ticks - 1)
				.Sum();
			int totalAdjustedTicks = DemoTickCounts
				.Where(tuple => tuple.adjustedTicks != int.MinValue)
				.Select(tuple => tuple.adjustedTicks - 1)
				.Sum();
			float tickInterval = CurDemo.DemoSettings.TickInterval;
			Console.WriteLine($"\n{"Total measured time ",  -25}: {FormatTime(totalTicks * tickInterval)}");
			Console.WriteLine($"{"Total measured ticks ",   -25}: {totalTicks}");
			if (totalTicks != totalAdjustedTicks) {
				Console.WriteLine($"{"Total adjusted time ",  -25}: {FormatTime(totalAdjustedTicks * tickInterval)}");
				Console.WriteLine($"{"Total adjusted ticks ", -25}: {totalAdjustedTicks}");
			}
			
			Console.ForegroundColor = originalColor;
		}


		private static void ConsFunc_RegexSearch(string pattern) {
			if (_runnableOptionCount > 1)
				Console.Write("Finding regex matches...");
			List<ConsoleCmd> matches = CurDemo.FilterForRegexMatches(new Regex(pattern)).ToList();
			if (matches.Count == 0) {
				Console.WriteLine(" no matches found");
			} else {
				if (_runnableOptionCount > 1)
					Console.WriteLine();
				SetTextWriter("regex matches");
				matches.ForEach(cmd => {_curTextWriter.WriteLine($"[{cmd.Tick}] {cmd.Command}");});
			}
		}


		private static void ConsFunc_DumpJumps() {
			SetTextWriter("jump dump");
			if (_runnableOptionCount > 1)
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
			if (_runnableOptionCount > 1)
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
			if (_runnableOptionCount > 1)
				Console.Write("Removing captions...");
			
			Packet[] closeCaptionPackets = CurDemo.FilterForPacket<Packet>()
				.Where(packet => packet.FilterForMessage<SvcUserMessageFrame>()
					.Any(frame => frame.UserMessageType == UserMessageType.CloseCaption)).ToArray();

			if (closeCaptionPackets.Length == 0) {
				Console.WriteLine(" no captions found");
				return;
			}
			SetBinaryWriter("captions_removed", "dem");
			Console.WriteLine();
			
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


		private static void ConsFunc_DumpDataTables(DataTablesDispType dispType) {
			SetTextWriter("data tables dump");
			if (_runnableOptionCount > 1)
				Console.WriteLine("Dumping data tables...");
			foreach ((DataTables tables, int j) in CurDemo.FilterForPacket<DataTables>().Select((tables, i) => (tables, i))) {
				if (j > 0)
					_curTextWriter.Write("\n\n\n");
				if (dispType == DataTablesDispType.Default) {
					_curTextWriter.WriteLine(tables.ToString());
				} else {
					var tableParser = new DataTableParser(CurDemo, tables);
					tableParser.FlattenClasses();
					foreach ((ServerClass sClass, List<FlattenedProp> fProps) in tableParser.FlattenedProps) {
						_curTextWriter.WriteLine($"{sClass.ClassName} ({sClass.DataTableName}) ({fProps.Count} props):");
						for (var i = 0; i < fProps.Count; i++) {
							FlattenedProp fProp = fProps[i];
							_curTextWriter.Write($"\t({i}): ");
							_curTextWriter.Write(fProp.TypeString().PadRight(12));
							_curTextWriter.WriteLine(fProp.ArrayElementProp == null
								? fProp.Prop.ToStringNoType()
								: fProp.ArrayElementProp.ToStringNoType());
						}
					}
				}
				_curTextWriter.Write("\n\nClass hierarchy:\n\n");
				_curTextWriter.Write(new DataTableTree(tables, true));
			}
		}


		private static void ConsFunc_DumpActions(ActPressedDispType opt) {
			SetTextWriter("actions pressed");
			if (_runnableOptionCount > 1)
				Console.WriteLine("Getting actions pressed...");
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


		// todo when I store references to entities, add portal color info here as well
		private static void ConsFunc_PortalsPassed(PtpFilterType filterType) {
			SetTextWriter("portals passed");
			if (_runnableOptionCount > 1)
				Console.Write("Finding passing through portals...");
			bool playerOnly = filterType == PtpFilterType.PlayerOnly || filterType == PtpFilterType.PlayerOnlyVerbose;
			bool verbose = filterType == PtpFilterType.PlayerOnlyVerbose || filterType == PtpFilterType.AllEntitiesVerbose;
			var msgList = CurDemo.FilterForUserMessage<EntityPortalled>().ToList();
			if (playerOnly)
				msgList = msgList.Where(tuple => tuple.userMessage.PortalledEntIndex == 1).ToList();
			if (msgList.Any()) {
				if (_runnableOptionCount > 1)
					Console.WriteLine();
				foreach ((EntityPortalled userMessage, int tick) in msgList) {
					_curTextWriter.Write($"[{tick}]");
					_curTextWriter.WriteLine(verbose
						? $"\n{userMessage.ToString()}"
						: $" entity {userMessage.PortalledEntIndex} went through portal {userMessage.PortalEntIndex}");
				}
			} else {
				string s = playerOnly ? "player never went through a portal" : "no entities went through portals";
				Console.WriteLine($" {s}");
				if (_curTextWriter != Console.Out)
					_curTextWriter.WriteLine(s);
			}
		}


		private static void ConsFunc_Errors() {
			if (_runnableOptionCount > 1)
				Console.Write("Getting errors during parsing... ");
			if (CurDemo.ErrorList.Count == 0) {
				Console.WriteLine("no errors");
			} else {
				Console.WriteLine();
				SetTextWriter("errors");
				CurDemo.ErrorList.ForEach(s => _curTextWriter.WriteLine(s));
			}
		}


		private static void ConsFunc_ChangeDemoDir(string dir) {
			Console.Write("Changing demo dir");
			string old = CurDemo.Header.GameDirectory;
			if (CurDemo.Header.GameDirectory == dir) {
				Console.WriteLine("... demo directories match!");
			} else {
				Console.Write($@" from ""{old}"" to ""{dir}""... ");
				SetBinaryWriter("changed_dir", "dem");
				int lenDiff = dir.Length - old.Length;
				BitStreamReader bsr = CurDemo.Reader;
				byte[] dirBytes = Encoding.ASCII.GetBytes(dir);
				
				_curBinWriter.Write(bsr.ReadBytes(796));
				_curBinWriter.Write(dirBytes); // header doesn't matter but I change it anyway
				_curBinWriter.Write(new byte[260 - dir.Length]);
				bsr.SkipBytes(260);
				_curBinWriter.Write(bsr.ReadBytes(12));
				// change header signOn byteCount; this number might be wrong if the endianness is big but whatever
				_curBinWriter.Write((uint)(bsr.ReadUInt() + lenDiff)); 

				foreach (SignOn signOn in CurDemo.FilterForPacket<SignOn>().Where(signOn => signOn.FilterForMessage<SvcServerInfo>().Any())) {
					// catch up to signOn packet
					int byteCount = (signOn.Reader.AbsoluteBitIndex - bsr.AbsoluteBitIndex) / 8;
					_curBinWriter.Write(bsr.ReadBytes(byteCount));
					bsr.SkipBits(signOn.Reader.BitLength);
					
					BitStreamWriter bsw = new BitStreamWriter();
					BitStreamReader signOnReader = signOn.Reader;
					bsw.WriteBits(signOnReader.ReadRemainingBits());
					signOnReader = signOnReader.FromBeginning();
					int bytesToMessageStreamSize = CurDemo.DemoSettings.SignOnGarbageBytes + 8;
					signOnReader.SkipBytes(bytesToMessageStreamSize);
					// edit the message stream length - read uint, and edit at index before the reading of said uint
					bsw.EditIntAtIndex((int)(signOnReader.ReadUInt() + lenDiff), signOnReader.CurrentBitIndex - 32, 32);
					
					// actually change the game dir
					SvcServerInfo serverInfo = signOn.FilterForMessage<SvcServerInfo>().Single();
					int editIndex = serverInfo.Reader.AbsoluteBitIndex - signOn.Reader.AbsoluteBitIndex + 186 + CurDemo.DemoSettings.SvcServerInfoUnknownBits;
					bsw.RemoveBitsAtIndex(editIndex, old.Length * 8);
					bsw.InsertBitsAtIndex(dirBytes, editIndex, dir.Length * 8);
					_curBinWriter.Write(bsw.AsArray);
				}
				_curBinWriter.Write(bsr.ReadRemainingBits().bytes);
				
				Console.WriteLine("done.");
			}
		}
		
		
		// todo add portals shot to csv


		private static void ConsFunc_LinkDemos() { // todo when this sets the writer, give it a custom name
			Console.WriteLine("Link demos feature not implemented yet.");
		}
		
		
		#endregion
	}
}