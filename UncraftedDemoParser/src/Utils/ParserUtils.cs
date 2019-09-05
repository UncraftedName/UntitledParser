using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UncraftedDemoParser.DemoStructure;
using UncraftedDemoParser.DemoStructure.Packets;

namespace UncraftedDemoParser.Utils {
	
	public static class ParserUtils {
		
		// returns a list of the packets and the ticks they're on
		public static List<Tuple<int, T>> FilterForPacketType<T>(this SourceDemo sd) where T : DemoComponent {
			return sd.Frames.Where(frame => frame.DemoComponent.GetType() == typeof(T))
				.Select(frame => Tuple.Create(frame.Tick, (T)frame.DemoComponent)).ToList();
		}
		
		
		private static Dictionary<Packet.SvcMessageType, int> GetSvcMessageDict(SourceDemo sd) {
			var messageCounter = new Dictionary<Packet.SvcMessageType, int>();
			foreach (Tuple<int, Packet> tuple in sd.FilterForPacketType<Packet>()) {
				Packet.SvcMessageType messageType = tuple.Item2.MessageType;
				if (messageCounter.ContainsKey(messageType))
					messageCounter[messageType]++;
				else
					messageCounter.Add(messageType, 1);
			}
			return messageCounter;
		}
		

		public static string CountSvcMessages(this SourceDemo sd) {
			var messageCounter = GetSvcMessageDict(sd);
			StringBuilder builder = new StringBuilder();
			foreach (Packet.SvcMessageType messageType in messageCounter.Keys.OrderBy(type => messageCounter[type]).ThenBy(type => type.ToString()))
				builder.AppendLine($"{messageType} shows up {messageCounter[messageType]} times");
			return builder.ToString();
		}


		public static string GetSubPacketData(this SourceDemo sd) {
			var messageCounter = GetSvcMessageDict(sd);
			StringBuilder builder = new StringBuilder();
			var packets = sd.FilterForPacketType<Packet>();
			foreach (Packet.SvcMessageType messageType in messageCounter.Keys.OrderBy(type => messageCounter[type]).ThenBy(type => type.ToString())) {
				builder.AppendLine($"-{messageType}-"); // append message name
				foreach ((int tick, byte[] data) in     // iterating over tick and the sub packet data
					packets.Where(packet => packet.Item2.MessageType == messageType)
						.Select(packet => Tuple.Create(packet.Item1, packet.Item2.SvcMessage))) 
				{
					builder.AppendLine($"[{tick}] {data.AsHexStr()}");
				}
			}
			return builder.ToString();
		}


		public static List<PacketFrame> ControlPacketFrameList(this SourceDemo sd) {
			return sd.Frames.Where(frame =>
				frame.Type == PacketType.SignOn || frame.Type == PacketType.StringTables ||
				frame.Type == PacketType.Packet || frame.Type == PacketType.Stop).ToList();
		}


		public static int Length(this SourceDemo sd) {
			List<int> packetTicks = sd.FilterForPacketType<Packet>().Select(tuple => tuple.Item1).Where(i => i >= 0).ToList();
			return packetTicks.Max() - packetTicks.Min() + 1;
		}


		public static List<int> TicksWhereRegexMatches(this SourceDemo sd, Regex regex) {
			return sd.FilterForPacketType<ConsoleCmd>().Where(tuple => regex.IsMatch(tuple.Item2.Command))
				.Select(tuple => tuple.Item1).ToList();
		}


		public static void PrintCustomOutput(this SourceDemo sd, bool printHeader, string demoName="") {
			if (!demoName.Equals("")) {
				Console.ForegroundColor = ConsoleColor.Gray;
				Console.WriteLine(demoName);
			}
			
			if (printHeader) {
				Header h = sd.Header;
				Console.ForegroundColor = ConsoleColor.Gray;
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