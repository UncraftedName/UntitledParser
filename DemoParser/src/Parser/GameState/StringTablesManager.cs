#nullable enable
using System.Collections.Generic;
using System.Linq;
using DemoParser.Parser.Components.Messages;
using DemoParser.Parser.Components.Packets;

namespace DemoParser.Parser.GameState {

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


	// Keeps track of string tables and their entries - these can be modified as a demo is parsed.
	internal class StringTablesManager { // todo any object taken from the tables should not be removed until the tables are updated for that tick

		private readonly SourceDemo _demoRef;
		private readonly List<MutableStringTable> _tablesList;

		// Before accessing any values in the tables, check to see if the respective table is in a valid state with IsTableStateValid().
		internal readonly Dictionary<string, MutableStringTable> Tables;

		// Ensuring that table data is accessed correctly is a relatively high priority for me, so if we fail a parse
		// with anything table related, the corresponding table is set to an invalid state. Otherwise, the table entries
		// and entry data is accurate to the best of my knowledge.
		private readonly ISet<string> _validTables;

		// I store this for later if there's a string tables packet, so that I can create the tables from this list
		// and the packet instead of the create message.
		internal readonly List<SvcCreateStringTable> CreationLookup;


		internal StringTablesManager(SourceDemo demoRef) {
			_demoRef = demoRef;
			_tablesList = new List<MutableStringTable>();
			Tables = new Dictionary<string, MutableStringTable>();
			_validTables = new HashSet<string>();
			CreationLookup = new List<SvcCreateStringTable>();
		}


		public bool IsTableStateValid(string tableName) => _validTables.Contains(tableName);
		public void SetTableStateInvalid(string tableName) => _validTables.Remove(tableName);


		internal MutableStringTable AddNewTableInfo(SvcCreateStringTable tableInfo) {
			CreationLookup.Add(tableInfo);
			_validTables.Add(tableInfo.TableName);
			return CreateNewTable(tableInfo);
		}


		private MutableStringTable CreateNewTable(SvcCreateStringTable tableInfo) {
			var table = new MutableStringTable(tableInfo);
			_tablesList.Add(table);
			Tables[tableInfo.TableName] = table;
			_validTables.Add(tableInfo.TableName);
			return table;
		}


		internal void Clear() {
			Tables.Clear();
			_validTables.Clear();
			_tablesList.Clear();
			CreationLookup.Clear();
		}


		internal MutableStringTable? TableFromId(int id) => id < 0 || id >= _tablesList.Count ? null : _tablesList[id];


		// we've gotten a StringTables packet, this is more reliable than SvcUpdateStringTables so use entries from it instead
		internal void InitFromPacket(StringTables tablesPacket) {
			_tablesList.Clear();
			Tables.Clear();
			foreach (StringTable table in tablesPacket.Tables) {
				MutableStringTable newTable;
				if (CreationLookup.Any()) {
					int id = CreationLookup.FindIndex(info => info.TableName == table.Name);
					if (id == -1) {
						_demoRef.LogError($"{GetType().Name}: could not find table '{table.Name}'");
						SetTableStateInvalid(table.Name);
						continue;
					}
					newTable = CreateNewTable(CreationLookup[id]);
				} else {
					// no SvcCreateStringTable (probably failed parsing), create a fake one so we can at least try to keep track of the table
					SvcCreateStringTable fakeInfo = new SvcCreateStringTable(null, 0) {
						TableName = table.Name,
						MaxEntries = -1,
						Flags = StringTableFlags.Fake,
						UserDataFixedSize = false,
						UserDataSize = -1,
						UserDataSizeBits = -1
					};
					newTable = AddNewTableInfo(fakeInfo);
				}

				table.MaxEntries = newTable.MaxEntries;

				if (IsTableStateValid(table.Name)) {

					if (table.TableEntries != null)
						foreach (StringTableEntry entry in table.TableEntries)
							Tables[table.Name].Entries.Add(entry);

					if (table.Classes != null)
						foreach (StringTableClass tableClass in newTable.Classes)
							Tables[table.Name].Classes.Add(tableClass);
				}
			}
		}
	}


	/*
	 * A StringTable that can be updated and changed as a demo is parsed. Specifically, new entries and classes can be
	 * added. According to game code, entries may be modified as well, but I haven't seen that. All entries/classes here
	 * should be treated as const since they are simply references to data that was parsed in the packet.
	 */
	public class MutableStringTable {

		// flattened fields from SvcCreateStringTable
		public readonly string Name;
		public readonly short MaxEntries;
		public readonly bool UserDataFixedSize;
		public readonly int UserDataSize;
		public readonly int UserDataSizeBits;
		public readonly StringTableFlags? Flags;
		// string table fields
		public readonly List<StringTableEntry> Entries;
		public readonly List<StringTableClass> Classes;


		public MutableStringTable(SvcCreateStringTable tableInfo) {
			Name              = tableInfo.TableName;
			MaxEntries        = tableInfo.MaxEntries;
			UserDataFixedSize = tableInfo.UserDataFixedSize;
			UserDataSize      = tableInfo.UserDataSize;
			UserDataSizeBits  = tableInfo.UserDataSizeBits;
			Flags             = tableInfo.Flags;
			Entries = new List<StringTableEntry>();
			Classes = new List<StringTableClass>();
		}


		public override string ToString() {
			return Name;
		}
	}
}
