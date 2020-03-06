#nullable enable
using System.Collections.Generic;
using UntitledParser.Parser.Components.Messages;
using UntitledParser.Parser.Components.Packets.StringTableEntryTypes;
using UntitledParser.Utils;

namespace UntitledParser.Parser.HelperClasses {

	// Keeps the original string tables passed here untouched, and keeps a separate "current copy"
	// since the tables can be updated/modified as the demo runs from SvcUpdateStringTable.
	internal class C_StringTablesManager {
		
		// the list here can be updated and is meant to be separate from the list in the stringtables packet
		private SourceDemo _demoRef;
		internal readonly List<C_StringTable> CurrentTables;
		internal readonly Dictionary<string, C_StringTable> TablesByName;

		// if the tables were parsed/updated without errors.
		// if set to false then anything in this class may be considered invalid
		private bool _readable;
		internal bool Readable {
			get => _readable;
			set {
				if (value == false)
					_demoRef.AddError("current tables set to be non-readable :(");
				_readable = value;
			}
		}

		internal C_StringTable DownloadablesTable 		=> TablesByName["downloadables"];
		internal C_StringTable ModelsTable 				=> TablesByName["modelprecache"];
		internal C_StringTable GenericTable 			=> TablesByName["genericprecache"];
		internal C_StringTable SoundTable 				=> TablesByName["soundprecache"];
		internal C_StringTable DecalTable 				=> TablesByName["decalprecache"];
		internal C_StringTable BaselineTableTable 		=> TablesByName["instancebaseline"];
		internal C_StringTable LightStylesTable 		=> TablesByName["lightstyles"];
		internal C_StringTable UserInfoTable 			=> TablesByName["userinfo"];
		internal C_StringTable ServerQueryTable 		=> TablesByName["server_query_info"];
		internal C_StringTable ParticleEffectsTable 	=> TablesByName["ParticleEffectNames"];
		internal C_StringTable EffectDispatchTable 		=> TablesByName["EffectDispatch"];
		internal C_StringTable VguiTable 				=> TablesByName["VguiScreen"];
		internal C_StringTable MaterialsTable 			=> TablesByName["Materials"];
		internal C_StringTable InfoPanelTable 			=> TablesByName["InfoPanel"];
		internal C_StringTable ScenesTable 				=> TablesByName["Scenes"];
		internal C_StringTable MeleeWeaponsTable 		=> TablesByName["MeleeWeapons"];
		internal C_StringTable GameRulesCreationTable 	=> TablesByName["GameRulesCreation"];
		internal C_StringTable BlackMarketTable 		=> TablesByName["BlackMarketTable"];


		internal C_StringTablesManager(SourceDemo demoRef) {
			_demoRef = demoRef;
			CurrentTables = new List<C_StringTable>();
			TablesByName = new Dictionary<string, C_StringTable>();
			Readable = true;
		}


		internal void CreateStringTable(SvcCreateStringTable creationInfo) {
			if (!Readable)
				return;
			var table = new C_StringTable(CurrentTables.Count, creationInfo);
			CurrentTables.Add(table);
			TablesByName[creationInfo.Name] = table;
		}


		internal void ClearCurrentTables() {
			if (!Readable)
				return;
			TablesByName.Clear();
			Readable = false;
			CurrentTables.Clear();
		}


		internal C_StringTable TableByID(int id) {
			return CurrentTables[id]; // should be right™
		}


		internal C_StringTableEntry AddTableEntry(C_StringTable table, BitStreamReader entryStream, string entryName) {
			if (!Readable)
				return null;
			// i assume this gets added to the end, but idk there's a reference to a tree structure or something
			table.Entries.Add(new C_StringTableEntry(_demoRef, table, entryStream, entryName));
			return table.Entries[^1];
		}


		internal C_StringTableEntry SetEntryData(C_StringTable table, C_StringTableEntry entry, BitStreamReader entryStream) {
			if (!Readable)
				return null;
			entry.EntryData = StringTableEntryDataFactory.CreateData(_demoRef, entryStream, table.Name, entry.EntryName);
			return entry;
		}
	}


	// a class separate from the StringTables packet for managing updateable tables, c stands for current
	public class C_StringTable {

		public int ID; // the index in the table list
		// flattened fields from SvcCreateStringTable
		public string Name;
		public ushort MaxEntries;
		public bool UserDataFixedSize;
		public int UserDataSize;
		public int UserDataSizeBits;
		public StringTableFlags? Flags;
		// string table fields
		public List<C_StringTableEntry> Entries;
		public List<C_StringTableClass> Classes;


		public C_StringTable(int id, SvcCreateStringTable creationInfo) {
			ID = id;
			Name = creationInfo.Name;
			MaxEntries = creationInfo.MaxEntries;
			UserDataFixedSize = creationInfo.UserDataFixedSize;
			UserDataSize = creationInfo.UserDataSize;
			UserDataSizeBits = (int)creationInfo.UserDataSizeBits;
			Flags = creationInfo.Flags;
			Entries = new List<C_StringTableEntry>();
			Classes = new List<C_StringTableClass>();
		}
	}


	public class C_StringTableEntry {
		
		private readonly SourceDemo _demoRef;
		private readonly C_StringTable _tableRef;
		public readonly string EntryName;
		public StringTableEntryData? EntryData;
		
		
		public C_StringTableEntry(SourceDemo demoRef, C_StringTable tableRef, BitStreamReader entryStream, string entryName) {
			_demoRef = demoRef;
			_tableRef = tableRef;
			EntryName = entryName;
			if (entryStream != null) {
				EntryData = StringTableEntryDataFactory.CreateData(demoRef, entryStream, tableRef.Name, entryName);
				EntryData.ParseOwnStream(); // do i parse now?
			}
		}
	}


	public class C_StringTableClass {
		
		public string Name;
		public string? Data;
	}
}