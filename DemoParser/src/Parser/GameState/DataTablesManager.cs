#nullable enable
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using DemoParser.Parser.Components.Messages;
using DemoParser.Parser.Components.Packets;
using DemoParser.Parser.Components.Packets.StringTableEntryTypes;
using DemoParser.Parser.EntityStuff;
using DemoParser.Utils;
using static DemoParser.Parser.EntityStuff.SendPropEnums;
using static DemoParser.Parser.EntityStuff.SendPropEnums.PropFlag;

namespace DemoParser.Parser.GameState {

	// https://github.com/StatsHelix/demoinfo/blob/ac3e820d68a5a76b1c4c86bf3951e9799f669a56/DemoInfo/DT/DataTableParser.cs#L69

	// expands the DataTables packet into a list of entity properties for each server class
	public class DataTablesManager {

		private readonly SourceDemo _demoRef;
		private readonly DataTables _dtRef;
		public int ServerClassBits => ParserUtils.HighestBitIndex((uint)_dtRef.ServerClasses.Count) + 1; // this might be off for powers of 2
		public PropLookup? FlattenedProps {get;private set;} // not initialized during the server class info



		public DataTablesManager(SourceDemo demoRef, DataTables dtRef) {
			_demoRef = demoRef;
			_dtRef = dtRef;
		}


		// if allow modify is set then we'll parse the baselines in the prior string tables
		public void FlattenClasses(bool allowModifyDemo) {
			var tableLookup = _dtRef.Tables.ToImmutableDictionary(table => table.Name, table => table);

			FlattenedProps = new PropLookup(_dtRef.ServerClasses.Count);

			foreach (ServerClass serverClass in _dtRef.ServerClasses) {
				SendTable table = _dtRef.Tables[serverClass.DataTableId];
				GatherProps(tableLookup, GatherExcludes(tableLookup, table), table, serverClass, "");
				SortProps(FlattenedProps[serverClass.DataTableId].flattenedProps);
			}

			// Now that I know the order of the props, I will parse the baselines of any SvcCreateMessages that
			// appeared BEFORE the datatables (valve really do be like that). In game, the baselines are stored as an
			// array and reparsed every time they're updated during demo playback. I just parse them once and store
			// them in a more accessible format.

			if (allowModifyDemo && _demoRef.State.StringTablesManager.IsTableStateValid(TableNames.InstanceBaseLine)) {
				_demoRef.State.StringTablesManager.Tables[TableNames.InstanceBaseLine]
					.Entries
					.Select(entry => entry.EntryData)
					.OfType<InstanceBaseline>()
					.ToList()
					.ForEach(baseline => baseline.ParseStream(baseline.Reader));
			}
		}


		private ISet<(string propName, string tableName)> GatherExcludes(IReadOnlyDictionary<string, SendTable> tableLookup, SendTable table) {
			var excludes = new HashSet<(string, string)>();
			foreach (SendTableProp sendProp in table.SendProps) {
				if (sendProp.SendPropType == SendPropType.DataTable)
					excludes.UnionWith(GatherExcludes(tableLookup, tableLookup[sendProp.ExcludeDtName!]));
				else if (_demoRef.DemoInfo.PropFlagChecker.HasFlag(sendProp.Flags, Exclude))
					excludes.Add((sendProp.ExcludeDtName!, sendProp.Name));
			}
			return excludes;
		}


		private void GatherProps(
			IReadOnlyDictionary<string, SendTable> tableLookup,
			ISet<(string, string)> excludes,
			SendTable table,
			ServerClass serverClass,
			string prefix)
		{
			List<FlattenedProp> fProps = new List<FlattenedProp>(table.SendProps.Count);
			IterateProps(tableLookup, excludes, table, serverClass, fProps, prefix);
			if (serverClass.DataTableId == FlattenedProps.Count)
				FlattenedProps.Add((serverClass, new List<FlattenedProp>()));
			FlattenedProps[serverClass.DataTableId].flattenedProps.AddRange(fProps);
		}


		private void IterateProps(
			IReadOnlyDictionary<string, SendTable> tableLookup,
			ISet<(string, string)> excludes,
			SendTable table,
			ServerClass serverClass,
			ICollection<FlattenedProp> fProps,
			string prefix)
		{
			for (int i = 0; i < table.SendProps.Count; i++) {
				SendTableProp prop = table.SendProps[i];
				if (_demoRef.DemoInfo.PropFlagChecker.HasFlags(prop.Flags, Exclude, InsideArray) || excludes.Contains((table.Name, prop.Name)))
					continue;
				if (prop.SendPropType == SendPropType.DataTable) {
					SendTable subTable = tableLookup[prop.ExcludeDtName!];
					// we don't prefix Collapsible stuff, since it is just derived mostly
					if (_demoRef.DemoInfo.PropFlagChecker.HasFlag(prop.Flags, Collapsible))
						IterateProps(tableLookup, excludes, subTable, serverClass, fProps, prefix);
					else
						GatherProps(tableLookup, excludes, subTable, serverClass, prop.Name.Length > 0 ? prop.Name + "." : "");
				} else {
					fProps.Add(new FlattenedProp(_demoRef.DemoInfo, prefix + prop.Name, prop,
						prop.SendPropType == SendPropType.Array ? table.SendProps[i - 1] : null));
				}
			}
		}


		// Properties that have a smaller index take up less bits in a delta, so some that are frequently updated get a
		// priority or a "ChangesOften" flag in newer versions which bumps them up in the table. This is done with a
		// custom non-stable sort.
		private void SortProps(IList<FlattenedProp> fProps) {
			if (_demoRef.DemoInfo.NewDemoProtocol && !_demoRef.DemoInfo.IsLeft4Dead1()) {
				int start = 0;
				var priorities = fProps.Select(entry => entry.PropInfo.Priority!.Value).Concat(new[] { 64 }).Distinct();
				foreach (int priority in priorities.OrderBy(i => i)) {
					for (int i = start; i < fProps.Count; i++) {
						SendTableProp p = fProps[i].PropInfo;
						// ChangesOften gets the same priority as 64
						if (p.Priority == priority || (_demoRef.DemoInfo.PropFlagChecker.HasFlag(p.Flags, ChangesOften) && priority == 64)) {
							if (start != i)
								(fProps[start], fProps[i]) = (fProps[i], fProps[start]);
							start++;
						}
					}
				}
			} else { // se2007/engine/dt.cpp line 443
				int start = 0;
				for (int i = start; i < fProps.Count; i++) {
					FlattenedProp p = fProps[i];
					if (_demoRef.DemoInfo.PropFlagChecker.HasFlag(p.PropInfo.Flags, ChangesOften)) {
						fProps[i] = fProps[start];
						fProps[start] = p;
						start++;
					}
				}
			}
		}
	}


	public class FlattenedProp {

		internal readonly DemoInfo DemoInfo;
		public readonly string Name;
		public readonly SendTableProp PropInfo;
		public readonly SendTableProp? ArrayElementPropInfo;
		// used for pretty ToString() representations, cached here
		private DisplayType? _displayType;
		internal DisplayType DisplayType => _displayType ??=
			EntPropToStringHelper.DeterminePropDisplayType(Name, PropInfo, ArrayElementPropInfo);


		public FlattenedProp(DemoInfo demoInfo, string name, SendTableProp propInfo, SendTableProp? arrayElementPropInfo) {
			PropInfo = propInfo;
			ArrayElementPropInfo = arrayElementPropInfo;
			DemoInfo = demoInfo;
			Name = name;
		}


		public override string ToString() {
			SendTableProp displayProp = DemoInfo.PropFlagChecker.HasFlag(PropInfo.Flags, InsideArray) ? ArrayElementPropInfo! : PropInfo;
			return $"{TypeString()} {Name}, " +
				   $"{displayProp.NumBits} bit{(displayProp.NumBits == 1 ? "" : "s")}, " +
				   $"flags: {displayProp.Flags}";
		}


		// if elements are of array type, converts from something like "int" to something like "int[32]"
		public string TypeString() {
			return PropInfo.SendPropType == SendPropType.Array
				? $"{ArrayElementPropInfo?.SendPropType.ToString().ToLower()}[{PropInfo.NumElements}]"
				: PropInfo.SendPropType.ToString().ToLower();
		}
	}


	// if only we had typedefs...
	public class PropLookup: List<(ServerClass serverClass, List<FlattenedProp> flattenedProps)> {
		public PropLookup(int capacity) : base(capacity) {}
	}
}
