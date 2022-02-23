#nullable enable
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.Components.Packets;
using DemoParser.Parser.GameState;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {

	public class SvcUpdateStringTable : DemoMessage {

		public byte TableId;
		public string? TableName; // i want to keep a copy of this if I convert to string later
		public ushort ChangedEntriesCount;
		public StringTableUpdates TableUpdates;


		public SvcUpdateStringTable(SourceDemo? demoRef, byte value) : base(demoRef, value) {}


		protected override void Parse(ref BitStreamReader bsr) {
			TableId = (byte)bsr.ReadUInt(5);
			var table = GameState.StringTablesManager.TableFromId(TableId);
			if (table == null) {
				bsr.SetOverflow();
				DemoRef.LogError($"{GetType().Name}: invalid table ID {TableId}");
				return;
			}
			TableName = table.Name;
			ChangedEntriesCount = bsr.ReadUShortIfExists() ?? 1;
			uint dataLen = bsr.ReadUInt(20);

			TableUpdates = new StringTableUpdates(DemoRef, TableName, ChangedEntriesCount, false);
			TableUpdates.ParseStream(bsr.SplitAndSkip((int)dataLen));
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

		private const int SubStringBits = 5;
		private const int MaxUserDataBits = 14;

		public bool? DictionaryActuallyEnabled;
		public readonly List<TableUpdate?> TableUpdates;

		private readonly int _numUpdatedEntries;
		private readonly string _tableName;
		private readonly bool _canCompress;
		private bool _failedParse;

		public static implicit operator List<TableUpdate?>(StringTableUpdates u) => u.TableUpdates;



		public StringTableUpdates(SourceDemo? demoRef, string tableName, int numUpdatedEntries, bool canCompress) : base(demoRef) {
			_tableName = tableName;
			_numUpdatedEntries = numUpdatedEntries;
			_canCompress = canCompress;
			TableUpdates = new List<TableUpdate?>(_numUpdatedEntries);
		}


		// CNetworkStringTable::ParseUpdate
		protected override void Parse(ref BitStreamReader bsr) {

			StringTablesManager manager = GameState.StringTablesManager;
			Debug.Assert(manager.CreationLookup.Single(table => table.TableName == _tableName).Flags != StringTableFlags.Fake);

			if (!manager.IsTableStateValid(_tableName)) {
				FailedTableUpdate($"{GetType().Name}: can't update string table '{_tableName}' (probably because a previous update failed)");
				return;
			}

			MutableStringTable mTable = manager.Tables[_tableName];

			// sometimes the updates are compressed (but only in SvcCreateStringTable)

			int? entryDecompIdx = null;
			if (mTable.Flags.HasValue && (mTable.Flags & StringTableFlags.DataCompressed) != 0 && _canCompress) {
				// decompress the data - engine/baseclientstate.cpp (hl2_src) line 1364
				int uncompressedSize = bsr.ReadSInt();
				int compressedSize = bsr.ReadSInt();
				// -8 to ignore the last 8 header bytes
				if (!Compression.Decompress(DemoRef!, ref bsr, compressedSize - 8, out byte[] data) || data.Length != uncompressedSize) {
					FailedTableUpdate($"{GetType().Name}: failed to decompress update");
					return;
				}
				entryDecompIdx = GameState.DecompressedLookup.Count;
				GameState.DecompressedLookup.Add(data);
				bsr = new BitStreamReader(data);
			}

			if (DemoInfo.NewDemoProtocol) {
				if ((DictionaryActuallyEnabled = bsr.ReadBool()) == true) {
					FailedTableUpdate($"{GetType().Name}: {_tableName} table encoded with dictionary");
					return;
				}
			}

			// Loop over all updates. The game keeps the last 32 entries as a basic way to (further) compress the entry
			// names (since consecutive entries often have the same prefix).

			int entryIndex = -1;
			var history = new C5.CircularQueue<string>(32);

			for (int i = 0; i < _numUpdatedEntries; i++) {

				entryIndex++;

				if (!bsr.ReadBool())
					entryIndex = (int)bsr.ReadUInt(ParserUtils.HighestBitIndex((uint)mTable.MaxEntries));

				if (entryIndex > mTable.Entries.Count) {
					// I think we fail here for non-p1/hl2 games which implies the bool above doesn't mean what I think it does.
					FailedTableUpdate($"{GetType().Name}: entry index {entryIndex} out of bounds for table {_tableName}, only {mTable.Entries.Count} entries");
					return;
				}

				string entryName = "";
				if (bsr.ReadBool()) {
					if (bsr.ReadBool()) { // the first part of the string may be the same as previous entries
						int index = (int)bsr.ReadUInt(5);
						if (index >= history.Count) {
							FailedTableUpdate($"{GetType().Name}: attempted to access a non-existent entry from the history");
							return;
						}
						int subStrLen = (int)bsr.ReadUInt(SubStringBits);
						if (subStrLen > history[index].Length) {
							FailedTableUpdate($"{GetType().Name}: tried to create a substring with {subStrLen - history[index].Length} too many characters from history");
							return;
						}
						entryName = history[index][..subStrLen];
						entryName += bsr.ReadNullTerminatedString();
					} else {
						entryName = bsr.ReadNullTerminatedString();
					}
				}

				BitStreamReader? entryStream = null;

				if (bsr.ReadBool()) {
					int entryLen = mTable.UserDataFixedSize
						? mTable.UserDataSizeBits
						: (int)bsr.ReadUInt(MaxUserDataBits) * 8;

					entryStream = bsr.SplitAndSkip(entryLen);
				}

				if (bsr.HasOverflowed) {
					FailedTableUpdate($"{GetType().Name}: reader overflowed");
					return;
				}

				// it might technically be invalid to have duplicate entry names, but uhhhhhh whatever

				StringTableEntry entry = new StringTableEntry(DemoRef, _tableName, _numUpdatedEntries, entryName);
				if (entryStream.HasValue)
					entry.ParseEntryData(entryStream.Value, entryDecompIdx);

				bool replace = entryIndex < mTable.Entries.Count;

				TableUpdates.Add(new TableUpdate(entry, replace ? TableUpdateType.ReplacementEntry : TableUpdateType.NewEntry, entryIndex));

				if (replace)
					mTable.Entries[entryIndex] = entry;
				else
					mTable.Entries.Add(entry);

				if (history.Count == 32)
					history.Dequeue();
				history.Push(entryName);
			}
		}


		private void FailedTableUpdate(string reason) {
			_failedParse = true;
			GameState.StringTablesManager.SetTableStateInvalid(_tableName); // prevent future updates
			DemoRef.LogError(reason);
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			if (DictionaryActuallyEnabled.HasValue)
				pw.AppendLine($"dictionary enabled: {DictionaryActuallyEnabled}");
			if (_failedParse) {
				pw.Append("failed to parse");
				return;
			}
			if (TableUpdates.Count == 0) {
				pw.Append("no entries");
			} else {
				int padCount = TableUpdates
					.Select(update => (update.Entry.Name, update.Entry.EntryData))
					.Where(t => !(t.EntryData?.ContentsKnown ?? true) || (t.EntryData?.InlineToString ?? true))
					.Select(t => t.Name.Length + 3)
					.DefaultIfEmpty(2)
					.Max();

				for (int i = 0; i < TableUpdates.Count; i++) {
					if (i != 0)
						pw.AppendLine();
					TableUpdates[i].PadCount = padCount;
					TableUpdates[i].EntryCount = TableUpdates.Count;
					TableUpdates[i].PrettyWrite(pw);
				}
			}
		}
	}


	public class TableUpdate : PrettyClass {

		// for pretty printing
		internal int PadCount;
		internal int EntryCount;

		public readonly StringTableEntry? Entry;
		public readonly int Index;
		public readonly TableUpdateType UpdateType;


		public TableUpdate(StringTableEntry? entry, TableUpdateType updateType, int index) {
			Entry = entry;
			UpdateType = updateType;
			Index = index;
		}


		public override void PrettyWrite(IPrettyWriter pw) { // similar logic to that in string tables
			pw.Append($"[{Index}] {ParserTextUtils.CamelCaseToUnderscore(UpdateType.ToString())}: {Entry.Name}");
			if (Entry?.EntryData != null) {
				if (Entry.EntryData.InlineToString && EntryCount == 1) {
					pw.Append("; ");
					Entry.EntryData.PrettyWrite(pw);
				} else {
					pw.FutureIndent++;
					if (Entry.EntryData.ContentsKnown && !Entry.EntryData.InlineToString)
						pw.AppendLine();
					else
						pw.PadLastLine(PadCount + 15, '.');
					Entry.EntryData.PrettyWrite(pw);
					pw.FutureIndent--;
				}
			}
		}
	}


	public enum TableUpdateType {
		NewEntry,
		ReplacementEntry
	}
}
