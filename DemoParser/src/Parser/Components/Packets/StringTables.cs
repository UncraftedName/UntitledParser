#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.Components.Packets.StringTableEntryTypes;
using DemoParser.Utils;

namespace DemoParser.Parser.Components.Packets {
	
	public class StringTables : DemoPacket {
		
		public List<StringTable> Tables;
		
		
		public StringTables(SourceDemo demoRef, BitStreamReader reader, int tick) : base(demoRef, reader, tick) {}


		internal override void ParseStream(BitStreamReader bsr) {
			uint dataLen = bsr.ReadUInt();
			int indexBeforeTables = bsr.CurrentBitIndex;
			byte tableCount = bsr.ReadByte();
			Tables = new List<StringTable>(tableCount);
			for (int i = 0; i < tableCount; i++) {
				Tables.Add(new StringTable(DemoRef, bsr));
				Tables[^1].ParseStream(bsr);
			}

			bsr.CurrentBitIndex = indexBeforeTables + (int)(dataLen << 3);
			SetLocalStreamEnd(bsr);
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		internal override void AppendToWriter(IndentedWriter iw) {
			foreach (StringTable table in Tables) {
				table.AppendToWriter(iw);
				iw.AppendLine();
			}
		}
	}
	
	
	
	public class StringTable : DemoComponent {

		public string Name;
		public List<StringTableEntry> TableEntries;
		public List<StringTableClass> Classes;
		internal ushort? MaxEntries;


		public StringTable(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			Name = bsr.ReadNullTerminatedString();
			ushort entryCount = bsr.ReadUShort();
			if (entryCount > 0)
				TableEntries = new List<StringTableEntry>(entryCount);
			for (int i = 0; i < entryCount; i++) {
				TableEntries.Add(new StringTableEntry(DemoRef, bsr, this));
				TableEntries[^1].ParseStream(bsr);
			}
			if (bsr.ReadBool()) {
				ushort classCount = bsr.ReadUShort();
				Classes = new List<StringTableClass>(classCount);
				for (int i = 0; i < classCount; i++) {
					Classes.Add(new StringTableClass(DemoRef, bsr));
					Classes[^1].ParseStream(bsr);
				}
			}
			SetLocalStreamEnd(bsr);
			
			if (DemoRef.CStringTablesManager.Readable)
				MaxEntries = DemoRef.CStringTablesManager.TablesByName[Name].MaxEntries;
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		internal override void AppendToWriter(IndentedWriter iw) {
			iw.Append($"table name: {Name}");
			iw.AddIndent();
			iw.AppendLine();
			if (TableEntries != null) {
				iw.Append($"{TableEntries.Count}/{MaxEntries ?? -1}");
				iw.Append($" table {(TableEntries.Count > 1 ? "entries" : "entry")}:");
				iw.AddIndent();
				int longestName = TableEntries.Max(entry => entry.Name.Length); // for padding and stuff
				foreach (StringTableEntry tableEntry in TableEntries) {
					tableEntry.PadLength = longestName;
					iw.AppendLine();
					tableEntry.AppendToWriter(iw);
				}
				iw.SubIndent();
			} else {
				iw.Append("no table entries");
			}
			iw.AppendLine();
			if (Classes != null) {
				iw.Append($"{Classes.Count} {(Classes.Count > 1 ? "classes" : "class")}:");
				iw.AddIndent();
				foreach (StringTableClass tableClass in Classes) {
					iw.AppendLine();
					tableClass.AppendToWriter(iw);
				}
				iw.SubIndent();
			}
			else {
				iw.Append("no table classes");
			}
			iw.SubIndent();
		}
	}
	
	
	
	public class StringTableEntry : DemoComponent {

		public string Name;
		public StringTableEntryData? EntryData;

		internal readonly StringTable TableRef; // so that i know the name of which table i'm in
		internal int PadLength; // used to convert to string


		public StringTableEntry(SourceDemo demoRef, BitStreamReader reader, StringTable tableRef) : base(demoRef, reader) {
			TableRef = tableRef;
		}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			Name = bsr.ReadNullTerminatedString();
			if (bsr.ReadBool()) {
				ushort dataLen = bsr.ReadUShort();
				EntryData = StringTableEntryDataFactory.CreateData(DemoRef, bsr.SubStream(dataLen << 3), TableRef.Name, Name);
				//EntryData.ParseStream(bsr.SubStream()); // use substream since data won't be read if i don't know how to read it
				EntryData.ParseOwnStream();
				bsr.SkipBytes(dataLen);
				SetLocalStreamEnd(bsr);
			}
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		internal override void AppendToWriter(IndentedWriter iw) {
			if (EntryData != null) {
				if (EntryData.ContentsKnown) {
					iw += Name;
					iw.AddIndent();
					iw.AppendLine();
					EntryData.AppendToWriter(iw);
					iw.SubIndent();
				} else {
					iw += Name.PadRight(PadLength + 2, '.');
					EntryData.AppendToWriter(iw);
				}
			} else {
				iw += Name;
			}
		}
	}
	
	
	
	public class StringTableClass : DemoComponent {

		public string Name;
		public string? Data;
		
		
		public StringTableClass(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			Name = bsr.ReadNullTerminatedString();
			if (bsr.ReadBool()) {
				ushort dataLen = bsr.ReadUShort();
				Data = bsr.ReadStringOfLength(dataLen);
			}
			SetLocalStreamEnd(bsr);
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		internal override void AppendToWriter(IndentedWriter iw) {
			iw += $"name: {Name}";
			if (Data != null)
				iw += $", data: {Data}";
		}
	}
}