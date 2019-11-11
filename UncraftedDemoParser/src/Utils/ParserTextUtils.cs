using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UncraftedDemoParser.Parser;
using UncraftedDemoParser.Parser.Components;
using UncraftedDemoParser.Parser.Components.Abstract;
using UncraftedDemoParser.Parser.Components.Packets;
using UncraftedDemoParser.Parser.Misc;

namespace UncraftedDemoParser.Utils {
	
	public static class ParserTextUtils {
		
		// svc packet type 1 appears x times, svc packet type 2 appears x times, etc. 
		public static string SvcMessagesCounterAsString(this SourceDemo sd) {
			var messageCounter = sd.GetSvcMessageDict();
			StringBuilder builder = new StringBuilder();
			foreach (SvcMessageType messageType in messageCounter.Keys.OrderBy(type => messageCounter[type]).ThenBy(type => type.ToString()))
				builder.AppendLine($"{messageType} appears {messageCounter[messageType]} times");
			return builder.ToString();
		}


		// for every svc packet type, prints the tick that packet occured on as well as the svc packet data as a hex string
		public static string SvcMessagesAsString(this SourceDemo sd) {
			var messageCounter = sd.GetSvcMessageDict();
			StringBuilder builder = new StringBuilder();
			var packets = sd.FilteredForPacketType<Packet>();
			foreach (SvcMessageType messageType in messageCounter.Keys.OrderBy(type => messageCounter[type]).ThenBy(type => type.ToString())) {
				builder.AppendLine($"-{messageType}-"); // append message name
				foreach ((int tick, byte[] data) in     // iterating over tick and the sub packet data
					packets.Where(packet => packet.MessageType == messageType)
						.Select(packet => Tuple.Create(packet.Tick, packet.SvcMessageBytes))) 
				{
					builder.AppendLine($"[{tick}] {data.AsHexStr()}");
				}
			}
			return builder.ToString();
		}

		
		public static List<ConsoleCmd> PacketsWhereRegexMatches(this SourceDemo sd, Regex regex) {
			return sd.FilteredForPacketType<ConsoleCmd>().Where(cmd => regex.IsMatch(cmd.Command)).ToList();
		}


		public static string AsVerboseString(this SourceDemo sd) {
			StringBuilder output = new StringBuilder(300 * sd.Frames.Count);
			output.AppendLine(sd.Header.ToString());
			output.AppendLine();
			
			sd.Frames.ForEach(frame => {
				output.Append(frame);
				if (frame.Type != PacketType.Stop)
					output.AppendLine();
			});
			return output.ToString();
		}


		// prints the same "wrong" tick count as listdemo+ for consistency, not including the 0th tick
		public static void PrintListdemoOutput(this SourceDemo sd, bool printHeader, bool printName, TextWriter writer = null) {
			if (!sd.FilteredForPacketType<Packet>().Any())
				throw new FailedToParseException("this demo is missing regular packets");
			if (writer == null)
				writer = Console.Out;
			Console.ForegroundColor = ConsoleColor.Gray;
			if (printName)
				writer.WriteLine(sd.Name);
			
			if (printHeader) {
				Header h = sd.Header;
				writer.Write(
					$"{"Demo protocol",     -25}: {h.DemoProtocol}" +
					$"\n{"Network protocol",-25}: {h.NetworkProtocol}" +
					$"\n{"Server name",     -25}: {h.ServerName}" +
					$"\n{"Client name",     -25}: {h.ClientName}" +
					$"\n{"Map name",        -25}: {h.MapName}" +
					$"\n{"Game directory",  -25}: {h.GameDirectory}" +
					$"\n{"Playback time",   -25}: {h.PlaybackTime:F3}" +
					$"\n{"Ticks",           -25}: {h.TickCount}" +
					$"\n{"Frames",          -25}: {h.FrameCount}" +
					$"\n{"SignOn Length",   -25}: {h.SignOnLength}\n\n");
			}
			
			Regex[] regexes = {
				new Regex("^autosave$"), 
				new Regex("^autosavedangerous$"), 
				new Regex("^autosavedangerousissafe$"), 
				new Regex("^echo #checkpoint#$", RegexOptions.IgnoreCase),
				new Regex("^echo #save#$", RegexOptions.IgnoreCase),
				new Regex("^startneurotoxins 99999$")
			};
			string[] names = { // all appended by "detected on tick..."
				"Autosavedangerous command",
				"Autosavedangerousissafe command",
				"Autosave command",
				"Checkpoint flag",
				"Save flag",
				"End of game"
			};
			ConsoleColor[] colors = {
				ConsoleColor.DarkYellow,
				ConsoleColor.DarkYellow,
				ConsoleColor.DarkYellow,
				ConsoleColor.Magenta,
				ConsoleColor.Yellow,
				ConsoleColor.Green
			};
			for (int i = 0; i < regexes.Length; i++) {
				foreach (ConsoleCmd cmd in sd.PacketsWhereRegexMatches(regexes[i]).DistinctBy(cmd => cmd.Tick)) {
					Console.ForegroundColor = colors[i];
					writer.WriteLine($"{names[i]} detected on tick {cmd.Tick}, time {cmd.Tick / sd.DemoSettings.TicksPerSeoncd:F3}");
				}
			}

			Console.ForegroundColor = ConsoleColor.Cyan;
			writer.WriteLine(
				$"\n{"Calculated total time",   -25}: {(sd.TickCount() - 1) / sd.DemoSettings.TicksPerSeoncd}" +
				$"\n{"Calculated total ticks",  -25}: {sd.TickCount() - 1}\n");
			Console.ForegroundColor = ConsoleColor.Gray;
		}
	}
}