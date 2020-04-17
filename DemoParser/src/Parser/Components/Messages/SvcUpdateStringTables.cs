#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.Components.Packets.StringTableEntryTypes;
using DemoParser.Parser.HelperClasses;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {
	
	public class SvcUpdateStringTables : DemoMessage {

		public byte TableId;
		public string? TableName; // i want to keep a copy of this if I convert to string later
		public ushort ChangedEntriesCount;
		public StringTableUpdate TableUpdate;


		public SvcUpdateStringTables(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			TableId = (byte)bsr.ReadBitsAsUInt(5);
			TableName = DemoRef.CStringTablesManager.TableById(TableId).Name;
			ChangedEntriesCount = bsr.ReadUShortIfExists() ?? 1;
			uint dataLen = bsr.ReadBitsAsUInt(20);

			TableUpdate = new StringTableUpdate(DemoRef, bsr.SubStream(dataLen), TableName, ChangedEntriesCount);
			TableUpdate.ParseOwnStream();
			
			bsr.SkipBits(dataLen);
			SetLocalStreamEnd(bsr);
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void AppendToWriter(IndentedWriter iw) {
			iw.Append(TableName != null ? $"table: {TableName}" : "table id:");
			iw.AppendLine($" ({TableId})");
			iw.AppendLine($"number of changed entries: {ChangedEntriesCount}");
			iw.Append("table update:");
			iw.AddIndent();
			iw.AppendLine();
			TableUpdate.AppendToWriter(iw);
			iw.SubIndent();
		}
	}
	


	public class StringTableUpdate : DemoComponent {
		
		private readonly int _updatedEntries; // just used when parsing
		private readonly string _tableName;
		private bool _exceptionWhileParsing;
		public readonly List<C_TableUpdate> TableUpdates;
		
		
		
		public StringTableUpdate(SourceDemo demoRef, BitStreamReader reader, string tableName, int updatedEntries) : base(demoRef, reader) {
			_tableName = tableName;
			_updatedEntries = updatedEntries;
			TableUpdates = new List<C_TableUpdate>(_updatedEntries);
		}
		
		
		internal override void ParseStream(BitStreamReader bsr) { 
			
			C_StringTablesManager manager = DemoRef.CStringTablesManager;
			if (!manager.TableReadable[_tableName]) {
				DemoRef.AddError($"{_tableName} table is marked as non-readable, can't update :/");
				_exceptionWhileParsing = true;
				return;
			}
			
			try { // se2007/engine/networkstringtable.cpp  line 595
				C_StringTable tableToUpdate = manager.Tables[_tableName];
				int entryIndex = -1;
				List<string> history = new List<string>();

				for (int i = 0; i < _updatedEntries; i++) {
					
					entryIndex++;
					if (!bsr.ReadBool()) {
						if (DemoRef.Header.NetworkProtocol > 14) // i'm actually not sure if this is where this goes
							throw new NotImplementedException($"encoded with dictionary");
						entryIndex = (int)bsr.ReadBitsAsUInt(BitUtils.HighestBitIndex(tableToUpdate.MaxEntries));
					}

					string entryName = null;
					if (bsr.ReadBool()) {
						if (bsr.ReadBool()) { // the first part of the string may be the same as for other entries
							int index = (int)bsr.ReadBitsAsUInt(5);
							int subStrLen = (int)bsr.ReadBitsAsUInt(5); // SUBSTRING_BITS
							entryName = history[index].Substring(0, subStrLen);
							entryName += bsr.ReadNullTerminatedString();
						} else {
							entryName = bsr.ReadNullTerminatedString();
						}
					}

					BitStreamReader entryStream = null;

					int streamLen = 0;
					if (bsr.ReadBool()) {
						if (tableToUpdate.UserDataFixedSize)
							streamLen = tableToUpdate.UserDataSizeBits;
						else
							streamLen = (int)bsr.ReadBitsAsUInt(DemoRef.DemoSettings.MaxUserDataBits) * 8;
						entryStream = bsr.SubStream(streamLen);
					}

					// Check if we are updating an old entry or adding a new one
					if (entryIndex < tableToUpdate.Entries.Count) {
						// if client-side then negative index, otherwise positive
						entryName = tableToUpdate.Entries[Math.Abs(entryIndex)].EntryName;
					} else { // Grow the table (entryIndex must be the next empty slot)
						entryName ??= ""; // avoid crash because of NULL strings
						int j = tableToUpdate.Entries.FindIndex(tableEntry => tableEntry.EntryName == entryName);
						if (j == -1) {
							TableUpdates.Add(new C_TableUpdate(
								manager.AddTableEntry(tableToUpdate, entryStream, entryName), 
								C_TableUpdateType.NewEntry));
						} else {
							TableUpdates.Add(new C_TableUpdate(
								manager.SetEntryData(tableToUpdate, tableToUpdate.Entries[j], entryStream), 
								C_TableUpdateType.ChangeEntryData));
						}
						bsr.SkipBits(streamLen);
					}

					if (history.Count > 31)
						history.RemoveAt(0);
					history.Add(entryName);
				}
			} catch (Exception e) {
				// there was an update I couldn't parse, assume this C_table contain irrelevant data from here
				DemoRef.AddError($"error while parsing {GetType().Name} for table {_tableName}: {e.Message}");
				_exceptionWhileParsing = true;
				manager.TableReadable[_tableName] = false;
			}
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void AppendToWriter(IndentedWriter iw) {
			if (_exceptionWhileParsing) {
				iw.Append("error while parsing");
				return;
			}
			if (TableUpdates.Count == 0) {
				iw.Append("no entries");
			} else {
				int padCount = TableUpdates
					.Select(update => (Name: update.TableEntry.EntryName, Data: update.TableEntry.EntryData))
					.Where(t => t.Data != null && t.Data.GetType() == typeof(StringTableEntryData))
					.Select(t => t.Name.Length + 2)
					.DefaultIfEmpty(2)
					.Max();
				
				for (var i = 0; i < TableUpdates.Count; i++) {
					if (i != 0)
						iw.AppendLine();
					TableUpdates[i].PadCount = padCount;
					TableUpdates[i].AppendToWriter(iw);
				}
			}
		}
	}


	public class C_TableUpdate {

		internal int PadCount; // just for toString()
		public readonly C_StringTableEntry TableEntry;
		public readonly C_TableUpdateType UpdateType;


		public C_TableUpdate(C_StringTableEntry tableEntry, C_TableUpdateType updateType) {
			TableEntry = tableEntry;
			UpdateType = updateType;
		}
		
		
		public void AppendToWriter(IndentedWriter iw) { // similar logic to that in string tables
			iw.Append($"{ParserTextUtils.CamelCaseToUnderscore(UpdateType.ToString())}: {TableEntry.EntryName}");
			if (TableEntry.EntryData != null) {
				iw.AddIndent();
				if (TableEntry.EntryData.ContentsKnown)
					iw.AppendLine();
				else
					iw.PadLastLine(PadCount + 15, '.');
				TableEntry.EntryData.AppendToWriter(iw);
				iw.SubIndent();
			}
		}


		public override string ToString() {
			var iw = new IndentedWriter();
			AppendToWriter(iw);
			return iw.ToString();
		}
	}


	public enum C_TableUpdateType {
		NewEntry,
		ChangeEntryData
	}
}