#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
			uint dataLen = bsr.ReadUInt();
			int indexBeforeTables = bsr.CurrentBitIndex;
			byte tableCount = bsr.ReadByte();
			Tables = new List<StringTable>(tableCount);
			for (int i = 0; i < tableCount; i++) {
				Tables.Add(new StringTable(DemoRef));
				Tables[^1].ParseStream(ref bsr);
			}

			bsr.CurrentBitIndex = indexBeforeTables + (int)(dataLen << 3);

			// if this packet exists make sure to create the C_tables after we parse this
			DemoRef.StringTablesManager.CreateTablesFromPacket(this);
		}


		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
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
					TableEntries.Add(new StringTableEntry(DemoRef,this));
					TableEntries[^1].ParseStream(ref bsr);
				}
			}
			if (bsr.ReadBool()) {
				ushort classCount = bsr.ReadUShort();
				Classes = new List<StringTableClass>(classCount);
				for (int i = 0; i < classCount; i++) {
					Classes.Add(new StringTableClass(DemoRef));
					Classes[^1].ParseStream(ref bsr);
				}
			}
		}


		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
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



	public class StringTableEntry : DemoComponent {

		public string Name;
		public StringTableEntryData? EntryData;

		internal readonly StringTable TableRef; // so that i know the name of which table i'm in
		internal int PadLength; // used to convert to string


		public StringTableEntry(SourceDemo? demoRef, StringTable tableRef) : base(demoRef) {
			TableRef = tableRef;
		}


		protected override void Parse(ref BitStreamReader bsr) {
			Name = bsr.ReadNullTerminatedString();
			if (bsr.ReadBool()) {
				ushort dataLen = bsr.ReadUShort();

				Debug.Assert(DemoRef.DataTableParser.FlattenedProps != null);

				EntryData = StringTableEntryDataFactory.CreateData(
					DemoRef,
					null,
					TableRef.Name,
					Name,
					DemoRef.DataTableParser.FlattenedProps);

				EntryData.ParseStream(bsr.SplitAndSkip(dataLen << 3));
			}
		}


		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			if (EntryData != null) {
				if (EntryData.ContentsKnown) {
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


		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.Append($"name: {Name}");
			if (Data != null)
				pw.Append($", data: {Data}");
		}
	}
}
