#nullable enable
using System.Collections.Generic;
using System.Linq;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Packets {

	/*
	 * This packet consists of a bunch of various tables most of which are used by other messages. This includes decals,
	 * default entity states, player info, etc. Since my main goal is to create a valid toString() representation
	 * of the entire demo, once I parse this packet I do not change it. However, the tables can be changed by
	 * special messages, so I keep a copy of the tables in the string tables manager which are mutable and contain
	 * minimal parsing code.
	 */

	/// <summary>
	/// Contains lookup tables for messages such as sounds and models.
	/// </summary>
	public class StringTables : DemoPacket {

		public List<StringTable> Tables;


		public StringTables(SourceDemo? demoRef, PacketFrame frameRef) : base(demoRef, frameRef) {}


		protected override void Parse(ref BitStreamReader bsr) {
			int byteLen = bsr.ReadSInt();
			int indexBeforeTables = bsr.CurrentBitIndex;
			byte tableCount = bsr.ReadByte();
			Tables = new List<StringTable>(tableCount);
			for (int i = 0; i < tableCount; i++) {
				var table = new StringTable(DemoRef);
				Tables.Add(table);
				table.ParseStream(ref bsr);
			}
			bsr.CurrentBitIndex = indexBeforeTables + byteLen * 8;

			// if this packet exists make sure to create the tables manager after we parse this
			GameState.StringTablesManager.InitFromPacket(this);
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			foreach (StringTable table in Tables) {
				table.PrettyWrite(pw);
				pw.AppendLine();
			}
		}
	}



	public class StringTable : DemoComponent {

		public string Name;
		public List<StringTableEntry>? TableEntries;
		public List<StringTableClass>? Classes;
		internal short? MaxEntries; // initialized in the manager


		public StringTable(SourceDemo? demoRef) : base(demoRef) {}


		protected override void Parse(ref BitStreamReader bsr) {
			Name = bsr.ReadNullTerminatedString();
			ushort entryCount = bsr.ReadUShort();
			if (entryCount > 0) {
				TableEntries = new List<StringTableEntry>(entryCount);
				for (int i = 0; i < entryCount; i++) {
					var entry = new StringTableEntry(DemoRef, Name, entryCount);
					TableEntries.Add(entry);
					entry.ParseStream(ref bsr);
				}
			}
			if (bsr.ReadBool()) {
				ushort classCount = bsr.ReadUShort();
				Classes = new List<StringTableClass>(classCount);
				for (int i = 0; i < classCount; i++) {
					var @class = new StringTableClass(DemoRef);
					Classes.Add(@class);
					@class.ParseStream(ref bsr);
				}
			}
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.Append($"table name: {Name}");
			pw.FutureIndent++;
			pw.AppendLine();
			if (TableEntries != null) {
				pw.Append($"{TableEntries.Count}/{MaxEntries ?? -1}");
				pw.Append($" table {(TableEntries.Count > 1 ? "entries" : "entry")}:");
				pw.FutureIndent++;
				int longestName = TableEntries.Max(entry => entry.Name.Length); // for padding and stuff
				foreach (StringTableEntry tableEntry in TableEntries) {
					tableEntry.PadLength = longestName;
					pw.AppendLine();
					tableEntry.PrettyWrite(pw);
				}
				pw.FutureIndent--;
			} else {
				pw.Append("no table entries");
			}
			pw.AppendLine();
			if (Classes != null) {
				pw.Append($"{Classes.Count} {(Classes.Count > 1 ? "classes" : "class")}:");
				pw.FutureIndent++;
				foreach (StringTableClass tableClass in Classes) {
					pw.AppendLine();
					tableClass.PrettyWrite(pw);
				}
				pw.FutureIndent--;
			}
			else {
				pw.Append("no table classes");
			}
			pw.FutureIndent--;
		}
	}



	// Haha yes. Demos may have StringTables packet(s) AND SvcCreateStringTable messages. This is used in both.
	public class StringTableEntry : DemoComponent {

		public string Name;
		public StringTableEntryData? EntryData;

		private readonly string _tableName; // entry data encoding is determined by the table we're in

		// for pretty printing
		internal int PadLength;
		private readonly int _newEntryCount; // this is used for SvcCreateStringTable & SvcUpdateStringTable


		public StringTableEntry(SourceDemo? demoRef, string tableName, int newEntryCount, string? name = null) : base(demoRef) {
			Name = name!;
			_tableName = tableName;
			_newEntryCount = newEntryCount;
		}


		// This should only be called from the StringTables packet, in that case the name of this entry and the entry
		// data is uncompressed and stored inline.
		protected override void Parse(ref BitStreamReader bsr) {
			Name = bsr.ReadNullTerminatedString();
			if (bsr.ReadBool()) {
				ushort byteLen = bsr.ReadUShort();
				if (bsr.HasOverflowed) {
					DemoRef.LogError($"{GetType()}: reader overflowed");
					return;
				}
				ParseEntryData(bsr.SplitAndSkip(byteLen * 8), null);
			}
		}


		// This may be called from SvcUpdate/Create StringTable - all of the entries may be compressed and the entry
		// names are always compressed.
		internal void ParseEntryData(BitStreamReader bsr, int? decompressedIndex) {
			EntryData = StringTableEntryDataFactory.CreateEntryData(DemoRef, decompressedIndex, _tableName, Name);
			EntryData.ParseStream(bsr);
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			if (EntryData != null) {
				if ((EntryData.ContentsKnown && !EntryData.InlineToString) || (EntryData.InlineToString && _newEntryCount == 1)) {
					pw.Append(Name);
					if (EntryData.InlineToString) {
						pw.Append(": ");
						EntryData.PrettyWrite(pw);
					} else {
						pw.FutureIndent++;
						pw.AppendLine();
						EntryData.PrettyWrite(pw);
						pw.FutureIndent--;
					}
				} else {
					pw.Append(Name.PadRight(PadLength + 2, '.'));
					EntryData.PrettyWrite(pw);
				}
			} else {
				pw.Append(Name);
			}
		}
	}



	public class StringTableClass : DemoComponent {

		public string Name;
		public string? Data;


		public StringTableClass(SourceDemo? demoRef) : base(demoRef) {}


		protected override void Parse(ref BitStreamReader bsr) {
			Name = bsr.ReadNullTerminatedString();
			if (bsr.ReadBool()) {
				ushort dataLen = bsr.ReadUShort();
				Data = bsr.ReadStringOfLength(dataLen);
			}
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.Append($"name: {Name}");
			if (Data != null)
				pw.Append($", data: {Data}");
		}
	}
}
