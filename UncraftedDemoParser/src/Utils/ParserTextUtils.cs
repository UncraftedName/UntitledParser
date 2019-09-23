using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UncraftedDemoParser.Parser;
using UncraftedDemoParser.Parser.Components;
using UncraftedDemoParser.Parser.Components.Abstract;
using UncraftedDemoParser.Parser.Components.Packets;

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


		// returns a list of ticks where the given regex matches the consolecmd packet
		public static IEnumerable<int> TicksWhereRegexMatches(this SourceDemo sd, Regex regex) {
			return sd.FilteredForPacketType<ConsoleCmd>().Where(cmd => regex.IsMatch(cmd.Command))
				.Select(cmd => cmd.Tick);
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


		// prints basically what listdemo prints, optionally prints the "correct" timing for length
		public static void PrintListdemoOutput(this SourceDemo sd, bool printHeader, bool printName) {
			Console.ForegroundColor = ConsoleColor.Gray;
			if (printName)
				Console.WriteLine(sd.Name);
			
			if (printHeader) {
				Header h = sd.Header;
				Console.Write(
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

			StringBuilder builder = new StringBuilder("\n");
			foreach (int i in sd.TicksWhereRegexMatches(new Regex("autosave")))
				builder.AppendLine($"Autosave command detected on tick {i}, time {i / sd.DemoSettings.TicksPerSeoncd:F3}");
			foreach (int i in sd.TicksWhereRegexMatches(new Regex("#save#", RegexOptions.IgnoreCase)))
				builder.AppendLine($"Save flag detected on tick {i}, time {i / sd.DemoSettings.TicksPerSeoncd:F3}");
			foreach (int i in sd.TicksWhereRegexMatches(new Regex("startneurotoxins 99999")))
				builder.AppendLine(
					$"End of normal game detected on tick {i}, time {i / sd.DemoSettings.TicksPerSeoncd:F3}");
			
			if (builder.Length > 1) {
				Console.ForegroundColor = ConsoleColor.DarkYellow;
				Console.Write(builder.ToString());
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine("(Use tick + 1 for timing to include the 0th tick)");
			}

			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.WriteLine(
				$"\n{"Calculated total time",   -25}: {sd.Length() / sd.DemoSettings.TicksPerSeoncd}" +
				$"\n{"Calculated total ticks",  -25}: {sd.Length()}\n");
			Console.ForegroundColor = ConsoleColor.Gray;
		}
	}
}