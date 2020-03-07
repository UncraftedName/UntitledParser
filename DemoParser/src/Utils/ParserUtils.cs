using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DemoParser.Parser;
using DemoParser.Parser.Components;
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


		/// <summary>
		/// Filters for a specific kind of message from a Packet packet.
		/// </summary>
		/// <param name="packet"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static IEnumerable<T> FilterForMessageType<T>(this Packet packet) where T : DemoMessage {
			return packet
				.MessageStream
				.Select(tuple => tuple.Item2)
				.Where(message => message != null && message.GetType() == typeof(T))
				.Cast<T>();
		}


		/// <summary>
		/// Filters for a specific type of message from a SignOn packet.
		/// </summary>
		/// <param name="signOn"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static IEnumerable<T> FilterForMessageType<T>(this SignOn signOn) where T : DemoMessage {
			return signOn
				.MessageStream
				.Select(tuple => tuple.Item2)
				.Where(message => message != null && message.GetType() == typeof(T))
				.Cast<T>();
		}


		/// <summary>
		/// Gets all message in the demo stored in the Packet packet or the SignOn packet,
		/// as well as the tick on which each message occured on.
		/// </summary>
		/// <param name="demo"></param>
		/// <returns></returns>
		public static IEnumerable<(MessageType messageType, DemoMessage message, int tick)> FilterForMessages(this SourceDemo demo) {
			foreach (DemoPacket packet in demo.Frames.Select(frame => frame.Packet)) {
				MessageStream m;
				Type t = packet.GetType();
				if (t == typeof(SignOn))
					m = ((SignOn)packet).MessageStream;
				else if (t == typeof(Packet))
					m = ((Packet)packet).MessageStream;
				else
					continue;
				foreach ((MessageType messageType, DemoMessage demoMessage) in m)
					yield return (messageType, demoMessage, packet.Tick);
			}
		}


		public static IEnumerable<(T message, int tick)> FilterForMessageType<T>(this SourceDemo demo) where T : DemoMessage {
			return FilterForMessages(demo)
				.Where(tuple => tuple.message != null && tuple.message.GetType() == typeof(T))
				.Select(tuple => ((T)tuple.message, tuple.tick));
		}


		public static IEnumerable<(DemoMessage message, int tick)> FilterForMessageType(this SourceDemo demo, MessageType messageType) {
			return FilterForMessages(demo)
				.Where(tuple => tuple.message != null && tuple.messageType == messageType)
				.Select(tuple => (tuple.message, tuple.tick));
		}


		public static IEnumerable<ConsoleCmd> FilterForRegexMatches(this SourceDemo demo, Regex regex) {
			return demo.FilterForPacketType<ConsoleCmd>().Where(cmd => regex.IsMatch(cmd.Command));
		}
		
		
		// https://stackoverflow.com/questions/489258/linqs-distinct-on-a-particular-property
		public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector) {
			HashSet<TKey> seenKeys = new HashSet<TKey>();
			foreach (T element in source)
				if (seenKeys.Add(keySelector(element)))
					yield return element;
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