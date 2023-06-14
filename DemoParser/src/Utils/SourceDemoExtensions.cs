using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DemoParser.Parser;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.Components.Messages;
using DemoParser.Parser.Components.Packets;

namespace DemoParser.Utils {

	public static class SourceDemoExtensions {

		public static IEnumerable<T> FilterForPacket<T>(this SourceDemo demo) where T : DemoPacket {
			return demo.Frames
				.Select(frame => frame.Packet)
				.OfType<T>();
		}


		/// <summary>
		/// Filters for a specific kind of message from a Packet packet.
		/// </summary>
		public static IEnumerable<T> FilterForMessage<T>(this Packet packet) where T : DemoMessage
			=> packet.MessageStream.OfType<T>();


		/// <summary>
		/// Filters for a specific kind of user message from a Packet packet.
		/// </summary>
		public static IEnumerable<T> FilterForUserMessage<T>(this Packet packet) where T : UserMessage {
			return packet.FilterForMessage<SvcUserMessage>()
				.Where(frame => frame.UserMessage.GetType() == typeof(T))
				.Select(frame => (T)frame.UserMessage);
		}


		/// <summary>
		/// Gets all message in the demo stored in the Packet packet or the SignOn packet,
		/// as well as the tick on which each message occured on.
		/// </summary>
		public static IEnumerable<(DemoMessage message, int tick)> FilterForMessages(this SourceDemo demo) {
			return demo.Frames.Select(frame => frame.Packet)
				.OfType<Packet>()
				.SelectMany(p => p.MessageStream, (p, msgs) => (msgs, p.Tick));
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


		// same as above but allows you to search for message types that don't have a dedicated class (e.g. empty messages)
		public static IEnumerable<(UserMessage userMessage, int tick)> FilterForUserMessage(this SourceDemo demo, UserMessageType type) {
			return
				from tup in demo.FilterForMessage<SvcUserMessage>()
				where tup.message.MessageType == type
				select (tup.message.UserMessage, tup.tick);
		}


		public static IEnumerable<(T entryData, int tick)> FilterForStEntryData<T>(this SourceDemo demo) where T : StringTableEntryData {
			return
				from tables in demo.FilterForPacket<StringTables>()
				from table in tables.Tables
				where table.TableEntries != null
				from entry in table.TableEntries!
				where entry?.EntryData is T
				select ((T)entry.EntryData!, tables.Tick);
		}


		// same as above but allows you to search for entry types that don't have a dedicated class
		public static IEnumerable<(StringTableEntryData, int tick)> FilterForStEntryData(this SourceDemo demo, string stringTableName) {
			return
				from tables in demo.FilterForPacket<StringTables>()
				from table in tables.Tables
				where table.TableEntries != null && table.Name == stringTableName
				from entry in table.TableEntries!
				select (entry.EntryData, tables.Tick);
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


		// these will eventually™ be moved to their own timing class probably maybe


		public static bool TotalTimeValid(this SourceDemo demo) {
			return demo.StartTick.HasValue && demo.EndTick.HasValue;
		}


		public static bool AdjustedTimeValid(this SourceDemo demo) {
			return demo.StartAdjustmentTick.HasValue && demo.EndAdjustmentTick.HasValue;
		}


		public static int TickCount(this SourceDemo demo, bool countFirstTick) {
			if (!demo.TotalTimeValid())
				throw new ArgumentException("the demo was probably not parsed correctly");
			return demo.EndTick!.Value - demo.StartTick!.Value + (countFirstTick ? 1 : 0);
		}


		public static int AdjustedTickCount(this SourceDemo demo, bool countFirstTick) {
			if (!demo.AdjustedTimeValid())
				throw new ArgumentException("the demo was probably not parsed correctly");
			return demo.EndAdjustmentTick!.Value - demo.StartAdjustmentTick!.Value + (countFirstTick ? 1 : 0);
		}
	}
}
