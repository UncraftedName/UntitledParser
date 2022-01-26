#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.HelperClasses;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {

	public class SvcUpdateStringTable : DemoMessage {

		public byte TableId;
		public string? TableName; // i want to keep a copy of this if I convert to string later
		public ushort ChangedEntriesCount;
		public StringTableUpdates TableUpdates;


		public SvcUpdateStringTable(SourceDemo? demoRef) : base(demoRef) {}


		protected override void Parse(ref BitStreamReader bsr) {
			TableId = (byte)bsr.ReadBitsAsUInt(5);
			TableName = DemoRef.CurStringTablesManager.TableById(TableId).Name;
			ChangedEntriesCount = bsr.ReadUShortIfExists() ?? 1;
			uint dataLen = bsr.ReadBitsAsUInt(20);

			TableUpdates = new StringTableUpdates(DemoRef, TableName, ChangedEntriesCount, false);
			TableUpdates.ParseStream(bsr.SplitAndSkip(dataLen));
		}


		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.Append(TableName != null ? $"table: {TableName}" : "table id:");
			pw.AppendLine($" ({TableId})");
			pw.AppendLine($"number of changed entries: {ChangedEntriesCount}");
			pw.Append("table update:");
			pw.FutureIndent++;
			pw.AppendLine();
			TableUpdates.PrettyWrite(pw);
			pw.FutureIndent--;
		}
	}



	public class StringTableUpdates : DemoComponent {

		private readonly int _numUpdatedEntries;
		private readonly string _tableName;
		private readonly bool _isSvcCreate;
		private bool _exceptionWhileParsing;
		public readonly List<TableUpdate?> TableUpdates;

		public static implicit operator List<TableUpdate?>(StringTableUpdates u) => u.TableUpdates;



		public StringTableUpdates(SourceDemo? demoRef, string tableName, int numUpdatedEntries, bool isSvcCreate) : base(demoRef) {
			_tableName = tableName;
			_numUpdatedEntries = numUpdatedEntries;
			_isSvcCreate = isSvcCreate;
			TableUpdates = new List<TableUpdate?>(_numUpdatedEntries);
		}


		protected override void Parse(ref BitStreamReader bsr) {

			CurStringTablesManager manager = DemoRef.CurStringTablesManager;
			if (!manager.TableReadable.GetValueOrDefault(_tableName)) {
				DemoRef.LogError($"{_tableName} table is marked as non-readable, can't update :/");
				_exceptionWhileParsing = true;
				bsr.SkipToEnd();
				return;
			}

			if (manager.CreationLookup.Single(table => table.TableName == _tableName).Flags == StringTableFlags.Fake) {
				DemoRef.LogError($"{_tableName} table was created manually - not parsed in SvcServerInfo");
				_exceptionWhileParsing = true;
				bsr.SkipToEnd();
				return;
			}

			try { // se2007/engine/networkstringtable.cpp  line 595
				CurStringTable tableToUpdate = manager.Tables[_tableName];

				int? decompressedIndex = null;
				if (tableToUpdate.Flags.HasValue && (tableToUpdate.Flags & StringTableFlags.DataCompressed) != 0 && _isSvcCreate) {
					// decompress the data - engine/baseclientstate.cpp (hl2_src) line 1364
					int uncompressedSize = bsr.ReadSInt();
					int compressedSize = bsr.ReadSInt();
					byte[] data = Compression.Decompress(ref bsr, compressedSize - 8); // -8 to ignore header
					decompressedIndex = DemoRef.DecompressedLookup.Count;
					DemoRef.DecompressedLookup.Add(data); // so that we can access the reader for the entries later
					if (data.Length != uncompressedSize)
						throw new Exception("could not decompress data in string table update");
					bsr = new BitStreamReader(data);
				}

				int entryIndex = -1;
				List<string> history = new List<string>();

				for (int i = 0; i < _numUpdatedEntries; i++) {
					entryIndex++;
					if (!bsr.ReadBool())
						entryIndex = (int)bsr.ReadBitsAsUInt(BitUtils.HighestBitIndex(tableToUpdate.MaxEntries));

					string? entryName = null;
					if (bsr.ReadBool()) {
						if (bsr.ReadBool()) { // the first part of the string may be the same as for other entries
							int index = (int)bsr.ReadBitsAsUInt(5);
							int subStrLen = (int)bsr.ReadBitsAsUInt(DemoInfo.SubStringBits);
							entryName = history[index][..subStrLen];
							entryName += bsr.ReadNullTerminatedString();
						} else {
							entryName = bsr.ReadNullTerminatedString();
						}
					}

					BitStreamReader? entryStream = null;

					if (bsr.ReadBool()) {
						int streamLen = tableToUpdate.UserDataFixedSize
							? tableToUpdate.UserDataSizeBits
							: (int)bsr.ReadBitsAsUInt(DemoInfo.MaxUserDataBits) * 8;

						entryStream = bsr.SplitAndSkip(streamLen);
					}

					// Check if we are updating an old entry or adding a new one
					if (entryIndex < tableToUpdate.Entries.Count) {
						// if client-side then negative index, otherwise positive
						entryName = tableToUpdate.Entries[Math.Abs(entryIndex)].EntryName;
					} else { // Grow the table (entryIndex must be the next empty slot)
						entryName ??= ""; // avoid crash because of NULL strings
						int j = tableToUpdate.Entries.FindIndex(tableEntry => tableEntry.EntryName == entryName);
						if (j == -1) {
							TableUpdates.Add(new TableUpdate(
								manager.AddTableEntry(tableToUpdate, ref entryStream, decompressedIndex, entryName),
								TableUpdateType.NewEntry,
								tableToUpdate.Entries.Count - 1)); // sub 1 since we update the table 2 lines up
						} else {
							TableUpdates.Add(new TableUpdate(
								manager.SetEntryData(tableToUpdate, tableToUpdate.Entries[j]),
								TableUpdateType.ChangeEntryData,
								j));
						}
					}

					if (history.Count > 31)
						history.RemoveAt(0);
					history.Add(entryName);
				}
			} catch (Exception e) {
				// there was an update I couldn't parse, assume this C_table contain irrelevant data from here
				DemoRef.LogError($"error while parsing {GetType().Name} for table {_tableName}: {e.Message}");
				_exceptionWhileParsing = true;
				manager.TableReadable[_tableName] = false;
			}
			bsr.SkipToEnd(); // in case of an exception I don't want this to be logged, otherwise assume the data parsed fine
		}


		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			if (_exceptionWhileParsing) {
				pw.Append("error while parsing");
				return;
			}
			if (TableUpdates.Count == 0) {
				pw.Append("no entries");
			} else {
				int padCount = TableUpdates
					.Select(update => (Name: update.TableEntry.EntryName, Data: update.TableEntry.EntryData))
					.Where(t => !(t.Data?.ContentsKnown ?? true))
					.Select(t => t.Name.Length + 2)
					.DefaultIfEmpty(2)
					.Max();

				for (int i = 0; i < TableUpdates.Count; i++) {
					if (i != 0)
						pw.AppendLine();
					TableUpdates[i].PadCount = padCount;
					TableUpdates[i].PrettyWrite(pw);
				}
			}
		}
	}


	public class TableUpdate : PrettyClass {

		internal int PadCount; // just for toString()
		public readonly CurStringTableEntry? TableEntry;
		public readonly int Index;
		public readonly TableUpdateType UpdateType;


		public TableUpdate(CurStringTableEntry? tableEntry, TableUpdateType updateType, int index) {
			TableEntry = tableEntry;
			UpdateType = updateType;
			Index = index;
		}


		public override void PrettyWrite(IPrettyWriter pw) { // similar logic to that in string tables
			pw.Append($"[{Index}] {ParserTextUtils.CamelCaseToUnderscore(UpdateType.ToString())}: {TableEntry.EntryName}");
			if (TableEntry?.EntryData != null) {
				if (TableEntry.EntryData.InlineToString) {
					pw.Append("; ");
					TableEntry.EntryData.PrettyWrite(pw);
				} else {
					pw.FutureIndent++;
					if (TableEntry.EntryData.ContentsKnown)
						pw.AppendLine();
					else
						pw.PadLastLine(PadCount + 15, '.');
					TableEntry.EntryData.PrettyWrite(pw);
					pw.FutureIndent--;
				}
			}
		}
	}


	public enum TableUpdateType {
		NewEntry,
		ChangeEntryData
	}
}
