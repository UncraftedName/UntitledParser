using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DemoParser.Parser;
using DemoParser.Parser.Components;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.Components.Messages;
using DemoParser.Parser.Components.Packets;

namespace DemoParser.Utils {
	
	public static class ParserUtils {
		
		public static IEnumerable<T> FilterForPacket<T>(this SourceDemo demo) where T : DemoPacket {
			return demo.Frames
				.Select(frame => frame.Packet)
				.Where(packet => packet.GetType() == typeof(T))
				.Cast<T>();
		}
		

		/// <summary>
		/// Filters for a specific kind of message from a Packet packet.
		/// </summary>
		public static IEnumerable<T> FilterForMessage<T>(this Packet packet) where T : DemoMessage {
			return packet
				.MessageStream
				.Select(tuple => tuple.message)
				.Where(message => message != null && message.GetType() == typeof(T))
				.Cast<T>();
		}
		

		/// <summary>
		/// Filters for a specific kind of user message from a Packet packet.
		/// </summary>
		public static IEnumerable<T> FilterForUserMessage<T>(this Packet packet) where T : SvcUserMessage {
			return packet.FilterForMessage<SvcUserMessageFrame>()
				.Where(frame => frame.SvcUserMessage.GetType() == typeof(T))
				.Select(frame => (T)frame.SvcUserMessage);
		}
		
		
		/// <summary>
		/// Filters for a specific type of message from a SignOn packet.
		/// </summary>
		public static IEnumerable<T> FilterForMessage<T>(this SignOn signOn) where T : DemoMessage {
			return signOn
				.MessageStream
				.Select(tuple => tuple.message)
				.Where(message => message != null && message.GetType() == typeof(T))
				.Cast<T>();
		}
		

		/// <summary>
		/// Gets all message in the demo stored in the Packet packet or the SignOn packet,
		/// as well as the tick on which each message occured on.
		/// </summary>
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
		
		
		public static IEnumerable<(T message, int tick)> FilterForMessage<T>(this SourceDemo demo) where T : DemoMessage {
			return FilterForMessages(demo)
				.Where(tuple => tuple.message != null && tuple.message.GetType() == typeof(T))
				.Select(tuple => ((T)tuple.message, tuple.tick));
		}
		
		
		public static IEnumerable<(T userMessage, int tick)> FilterForUserMessage<T>(this SourceDemo demo) where T : SvcUserMessage {
			return demo.FilterForMessage<SvcUserMessageFrame>()
				.Where(tuple => tuple.message.SvcUserMessage.GetType() == typeof(T))
				.Select(tuple => ((T)tuple.message.SvcUserMessage, tuple.tick));
		}
		
		
		public static IEnumerable<ConsoleCmd> FilterForRegexMatches(this SourceDemo demo, Regex regex) {
			return demo.FilterForPacket<ConsoleCmd>().Where(cmd => regex.IsMatch(cmd.Command));
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
				.FilterForPacket<Packet>()
				.Select(packet => packet.Tick)
				.Where(i => i >= 0)
				.ToList();
			return packetTicks.Max() - packetTicks.Min() + 1; // accounts for 0th tick
		}
	}
}