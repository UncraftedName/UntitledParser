using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DemoParser.Parser;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.Components.Packets;

namespace DemoParser.Utils {
	
	public static class ParserUtils {

		public static IEnumerable<T> FilterForPacketType<T>(this SourceDemo demo) where T : DemoPacket {
			return demo.Frames
				.Select(frame => frame.Packet)
				.Where(packet => packet.GetType() == typeof(T))
				.Cast<T>();
		}


		public static IEnumerable<T> FilterForMessageType<T>(this Packet packet) where T : DemoMessage {
			return packet
				.MessageStream
				.Messages
				.Select(tuple => tuple.Item2)
				.Where(message => message != null && message.GetType() == typeof(T))
				.Cast<T>();
		}


		public static IEnumerable<T> FilterForMessageType<T>(this SourceDemo demo) where T : DemoMessage {
			return demo.FilterForPacketType<Packet>()
				.SelectMany(packet => packet.MessageStream.Messages)
				.Select(tuple => tuple.Item2)
				.Where(message => message != null && message.GetType() == typeof(T))
				.Cast<T>();
		}


		public static IEnumerable<(int tick, T message)> FilterForMessageTypeWithTicks<T>(this SourceDemo demo)
			where T : DemoMessage // I barely understand this select many linq stuff - rider just did everything for me
		{
			return demo.FilterForPacketType<Packet>()
				.SelectMany(FilterForMessageType<T>, (packet, message) => (packet.Tick, message));
		}


		public static IEnumerable<ConsoleCmd> FilterForRegexMatches(this SourceDemo demo, Regex regex) {
			return demo.FilterForPacketType<ConsoleCmd>().Where(cmd => regex.IsMatch(cmd.Command));
		}
		
		
		// https://stackoverflow.com/questions/489258/linqs-distinct-on-a-particular-property
		public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector) {
			HashSet<TKey> seenKeys = new HashSet<TKey>();
			foreach (T element in source) {
				if (seenKeys.Add(keySelector(element)))
					yield return element;
			}
		}
		
		
		public static int TickCount(this SourceDemo demo) {
			List<int> packetTicks = demo
				.FilterForPacketType<Packet>()
				.Select(packet => packet.Tick)
				.Where(i => i >= 0)
				.ToList();
			return packetTicks.Max() - packetTicks.Min() + 1; // accounts for 0th tick
		}
	}
}