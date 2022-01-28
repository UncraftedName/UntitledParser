#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.Components.Messages;
using DemoParser.Parser.Components.Packets;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.HelperClasses.GameState {

	public static class TableNames {
		public const string Downloadables       = "downloadables";
		public const string ModelPreCache       = "modelprecache";
		public const string GenericPreCache     = "genericprecache";
		public const string SoundPreCache       = "soundprecache";
		public const string DecalPreCache       = "decalprecache";
		public const string InstanceBaseLine    = "instancebaseline";
		public const string LightStyles         = "lightstyles";
		public const string UserInfo            = "userinfo";
		public const string ServerQueryInfo     = "server_query_info";
		public const string ParticleEffectNames = "ParticleEffectNames";
		public const string EffectDispatch      = "EffectDispatch";
		public const string VguiScreen          = "VguiScreen";
		public const string Materials           = "Materials";
		public const string InfoPanel           = "InfoPanel";
		public const string Scenes              = "Scenes";
		public const string MeleeWeapons        = "MeleeWeapons";
		public const string GameRulesCreation   = "GameRulesCreation";
		public const string BlackMarket         = "BlackMarketTable";
		// custom?
		public const string DynamicModels       = "DynamicModels";
		public const string ServerMapCycle      = "ServerMapCycle";
	}


	// Keeps the original string tables passed here untouched, and keeps a separate mutable copy
	// since the tables can be updated/modified as the parser reads from SvcUpdateStringTable.
	internal class StringTablesManager { // todo any object taken from the tables should not be taken until the tables are updated for that tick

		// the list here can be updated and is meant to be separate from the list in the stringtables packet
		private readonly SourceDemo _demoRef;
		private readonly List<MutableStringTable> _privateTables;
		internal readonly Dictionary<string, MutableStringTable> Tables;

		// Before accessing any values in the tables, check to see if the respective table is readable first,
		// make sure to use GetValueOrDefault() - this will ensure that even if the corresponding SvcCreationStringTable
		// message didn't parse there won't be an exception.
		internal readonly Dictionary<string, bool> TableReadable;

		// I store this for later if there's a string tables packet, so that I can create the tables from this list
		// and the packet instead of the create message.
		internal readonly List<SvcCreateStringTable> CreationLookup;


		internal StringTablesManager(SourceDemo demoRef) {
			_demoRef = demoRef;
			_privateTables = new List<MutableStringTable>();
			Tables = new Dictionary<string, MutableStringTable>();
			TableReadable = new Dictionary<string, bool>();
			CreationLookup = new List<SvcCreateStringTable>();
		}


		internal MutableStringTable CreateStringTable(SvcCreateStringTable creationInfo) {
			CreationLookup.Add(creationInfo);
			TableReadable[creationInfo.TableName] = true;
			return InitNewTable(_privateTables.Count, creationInfo);
		}


		private MutableStringTable InitNewTable(int id, SvcCreateStringTable creationInfo) {
			var table = new MutableStringTable(id, creationInfo);
			_privateTables.Add(table);
			Tables[creationInfo.TableName] = table;
			TableReadable[creationInfo.TableName] = true;
			return table;
		}


		internal void ClearCurrentTables() {
			Tables.Clear();
			TableReadable.Clear();
			_privateTables.Clear();
			CreationLookup.Clear();
		}


		internal MutableStringTable TableById(int id) {
			return _privateTables[id]; // should be right™
		}


		internal MutableStringTableEntry? AddTableEntry(
			MutableStringTable table,
			ref BitStreamReader? entryStream,
			int? decompressedIndex,
			string entryName)
		{
			var entry = AddTableEntry(table, null, entryName);

			if (entryStream.HasValue && entry != null) {
				entry.EntryData = StringTableEntryDataFactory.CreateData(
					_demoRef, decompressedIndex, table.Name, entryName, _demoRef.DataTableParser?.FlattenedProps);

				entry.EntryData.ParseStream(entryStream.Value);
			}
			return entry;
		}


		internal MutableStringTableEntry? AddTableEntry(MutableStringTable table, StringTableEntryData? eData,
			string entryName)
		{
			if (!TableReadable[table.Name])
				return null;
			table.Entries.Add(new MutableStringTableEntry(_demoRef, table, eData, entryName));
			return table.Entries[^1];
		}


		private void AddTableClass(MutableStringTable table, string name, string? data) {
			if (!TableReadable[table.Name])
				return;
			var stc = new MutableStringTableClass(name, data);
			table.Classes.Add(stc);
		}


		internal MutableStringTableEntry? SetEntryData(MutableStringTable table, MutableStringTableEntry entry) {
			if (!TableReadable[table.Name])
				return null;
			entry.EntryData = StringTableEntryDataFactory.CreateData(_demoRef, null, table.Name, entry.EntryName);
			return entry;
		}


		internal void CreateTablesFromPacket(StringTables tablesPacket) {
			_privateTables.Clear();
			TableReadable.Clear();
			bool useCreationLookup = CreationLookup.Any();
			try {
				foreach (StringTable table in tablesPacket.Tables) {
					MutableStringTable newTable;
					if (useCreationLookup) {
						int tableId = CreationLookup.FindIndex(info => info.TableName == table.Name);
						newTable = InitNewTable(tableId, CreationLookup[tableId]);
					} else {
						// create fake creation info
						SvcCreateStringTable fakeInfo = new SvcCreateStringTable(null) {
							TableName = table.Name,
							MaxEntries = -1,
							Flags = StringTableFlags.Fake,
							UserDataFixedSize = false,
							UserDataSize = -1,
							UserDataSizeBits = -1
						};
						// SvcServerInfo wasn't parsed correctly, assume i is the proper table ID
						newTable = CreateStringTable(fakeInfo);
					}

					table.MaxEntries = newTable.MaxEntries;

					if (table.TableEntries != null)
						foreach (StringTableEntry entry in table.TableEntries)
							AddTableEntry(newTable, entry?.EntryData?.CreateCopy(), entry.Name);
					if (table.Classes != null)
						foreach (MutableStringTableClass tableClass in newTable.Classes)
							AddTableClass(newTable, tableClass.Name, tableClass.Data);
				}
			} catch (Exception e) {
				_demoRef.LogError($"error while converting tables packet to c_tables: {e.Message}");
				TableReadable.Keys.ToList().ForEach(s => TableReadable[s] = false);
			}
		}
	}


	// classes separate from the StringTables packet for managing updateable tables
	// todo a whole lot of this stuff should be redone cuz even I don't know what the fuck is going on anymore


	public class MutableStringTable {

		public int Id; // the index in the table list
		// flattened fields from SvcCreateStringTable
		public readonly string Name;
		public readonly short MaxEntries;
		public readonly bool UserDataFixedSize;
		public readonly int UserDataSize;
		public readonly int UserDataSizeBits;
		public readonly StringTableFlags? Flags;
		// string table fields
		public readonly List<MutableStringTableEntry> Entries;
		public readonly List<MutableStringTableClass> Classes;


		public MutableStringTable(int id, SvcCreateStringTable creationInfo) {
			Id = id;
			Name = creationInfo.TableName;
			MaxEntries = creationInfo.MaxEntries;
			UserDataFixedSize = creationInfo.UserDataFixedSize;
			UserDataSize = creationInfo.UserDataSize;
			UserDataSizeBits = creationInfo.UserDataSizeBits;
			Flags = creationInfo.Flags;
			Entries = new List<MutableStringTableEntry>();
			Classes = new List<MutableStringTableClass>();
		}


		public override string ToString() {
			return Name;
		}
	}


	public class MutableStringTableEntry {

		private readonly SourceDemo _demoRef;
		private readonly MutableStringTable _tableRef;
		public readonly string EntryName;
		public StringTableEntryData? EntryData;


		public MutableStringTableEntry(SourceDemo demoRef, MutableStringTable tableRef, StringTableEntryData? eData, string entryName) {
			_demoRef = demoRef;
			_tableRef = tableRef;
			EntryData = eData;
			EntryName = entryName;
		}


		public override string ToString() {
			return EntryName;
		}
	}


	public class MutableStringTableClass {

		public readonly string Name;
		public readonly string? Data;


		public MutableStringTableClass(string name, string? data) {
			Name = name;
			Data = data;
		}


		public override string ToString() {
			return Name;
		}
	}
}
