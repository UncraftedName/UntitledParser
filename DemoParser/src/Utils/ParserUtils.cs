using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using DemoParser.Parser;
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
		public static IEnumerable<T> FilterForUserMessage<T>(this Packet packet) where T : UserMessage {
			return packet.FilterForMessage<SvcUserMessage>()
				.Where(frame => frame.UserMessage.GetType() == typeof(T))
				.Select(frame => (T)frame.UserMessage);
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
			return demo.Frames.Select(frame => frame.Packet)
				.OfType<IContainsMessageStream>()
				.SelectMany(p => p.MessageStream, 
					(p, msgs) => (msgs.messageType, msgs.message, ((DemoPacket)p).Tick));
		}
		
		
		public static IEnumerable<(T message, int tick)> FilterForMessage<T>(this SourceDemo demo) where T : DemoMessage {
			return 
				from tup in FilterForMessages(demo)
				where tup.message is T
				select ((T)tup.message, tup.tick);
		}
		
		
		public static IEnumerable<(T userMessage, int tick)> FilterForUserMessage<T>(this SourceDemo demo) where T : UserMessage {
			return 
				from tup in demo.FilterForMessage<SvcUserMessage>()
				where tup.message.UserMessage is T
				select ((T)tup.message.UserMessage, tup.tick);
		}


		public static IEnumerable<(T entryData, int tick)> FilterForStEntryData<T>(this SourceDemo demo) where T : StringTableEntryData {
			return 
				from tables in demo.FilterForPacket<StringTables>()
				from table in tables.Tables
				where table.TableEntries != null
				from entry in table.TableEntries
				where entry?.EntryData is T
				select ((T)entry.EntryData!, tables.Tick);
		}
		
		
		public static IEnumerable<(ConsoleCmd cmd, MatchCollection matches)> CmdRegexMatches(this SourceDemo demo, Regex re) {
			return demo.FilterForPacket<ConsoleCmd>()
				.Select(cmd => (cmd, matches: re.Matches(cmd)))
				.Where(t => t.matches.Count != 0);
		}


		public static IEnumerable<(ConsoleCmd cmd, MatchCollection matches)> CmdRegexMatches(
			this SourceDemo demo,
			string re,
			RegexOptions options = RegexOptions.None)
		{
			return demo.FilterForPacket<ConsoleCmd>()
				.Select(consoleCmd => (consoleCmd, matches: Regex.Matches(consoleCmd, re, options)))
				.Where(t => t.matches.Count != 0);
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
		

		internal static Dictionary<T, int> CreateReverseLookupDict<T>(
			this IEnumerable<T> lookup,
			T? excludeKey = null)
			where T : struct, Enum 
		{
			var indexed = lookup.Select((@enum,  index) => (index, @enum)).ToList();
			if (excludeKey != null)
				indexed = indexed.Where(t => !Equals(excludeKey, t.@enum)).ToList();
			return indexed.ToDictionary(t => t.@enum, t => t.index);
		}


		public static TV GetValueOrDefault<TK, TV>(this IDictionary<TK, TV> dict, TK key, TV defVal = default) {
			return dict.TryGetValue(key, out TV val) ? val : defVal;
		}


		public class ReferenceComparer<T> : IEqualityComparer<T> {
			public bool Equals(T a, T b) => ReferenceEquals(a, b);
#pragma warning disable CS0436
			public int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);
#pragma warning restore CS0436
		}


		public static unsafe T ByteSpanToStruct<T>(Span<byte> bytes) where T : struct {
			fixed (byte* ptr = bytes)
				return Marshal.PtrToStructure<T>((IntPtr)ptr)!;
		}
	}


	public readonly struct EHandle { // todo ref to entities, maybe add special behavior for --portal-passed
			
		public readonly uint Val;
		
		public static implicit operator uint(EHandle h) => h.Val;
		public static explicit operator EHandle(uint u) => new EHandle(u);
		public static explicit operator EHandle(int i) => new EHandle((uint)i);
			
		public EHandle(uint val) {
			Val = val;
		}

		public int EntIndex => (int)(Val & ((1 << DemoSettings.MaxEdictBits) - 1));
		public int Serial => (int)(Val >> DemoSettings.MaxEdictBits);

		public override string ToString() {
			return Val == DemoSettings.NullEHandle ? "{null}" : $"{{ent index: {EntIndex}, serial: {Serial}}}";
		}
	}


	public class ParseException : Exception {
		public ParseException(string? msg = null) : base(msg) {}
	}
}
