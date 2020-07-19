using System;
using System.Collections.Generic;
using System.Diagnostics;
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
				.OfType<T>();
		}
		

		/// <summary>
		/// Filters for a specific kind of message from a Packet packet.
		/// </summary>
		public static IEnumerable<T> FilterForMessage<T>(this Packet packet) where T : DemoMessage {
			return packet
				.MessageStream
				.Select(tuple => tuple.message)
				.Where(message => message != null)
				.OfType<T>();
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
				.Where(message => message != null)
				.OfType<T>();
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
		
		
		/// <summary>
		/// If you want this to behave the same way as other times, subtract 1
		/// </summary>
		public static int TickCount(this SourceDemo demo) {
			if (demo.StartTick == -1 || demo.EndTick == -1)
				throw new ArgumentException("the demo was probably not parsed correctly");
			return demo.EndTick - demo.StartTick + 1;
		}

		/// <summary>
		/// If you want this to behave the same way as other times, subtract 1
		/// </summary>
		public static int AdjustedTickCount(this SourceDemo demo) {
			if (demo.StartAdjustmentTick == -1 || demo.EndAdjustmentTick == -1)
				throw new ArgumentException("the demo was probably not parsed correctly");
			return demo.EndAdjustmentTick - demo.StartAdjustmentTick + 1;
		}


		internal static Dictionary<T, int> CreateReverseLookupDict<T>(this ICollection<T> lookup) {
			Debug.Assert(lookup.Count == lookup.Distinct().Count(), "lookup contains duplicate values");
			return lookup.Select((v,  i) => (v, i)).ToList().ToDictionary(tup => tup.v, tup => tup.i);
		}


		// if these consts change depending on versions this will have to be fixed
		public static bool IsNullEHandle(this int val) {
			return val == (1 << (DemoSettings.MaxEdictBits + DemoSettings.NumNetworkedEHandleBits)) - 1;
		}
	}
}