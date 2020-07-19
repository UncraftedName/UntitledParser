#nullable enable
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using DemoParser.Parser.Components.Messages;
using DemoParser.Parser.Components.Packets;
using DemoParser.Parser.Components.Packets.StringTableEntryTypes;
using DemoParser.Utils;
using static DemoParser.Parser.HelperClasses.EntityStuff.PropFlag;

namespace DemoParser.Parser.HelperClasses.EntityStuff {
	
	// https://github.com/StatsHelix/demoinfo/blob/ac3e820d68a5a76b1c4c86bf3951e9799f669a56/DemoInfo/DT/DataTableParser.cs#L69
	public class DataTableParser {

		private readonly SourceDemo _demoRef;
		private DemoSettings DemSet => _demoRef.DemoSettings;
		private readonly DataTables _dtRef;
		private readonly ImmutableDictionary<string, SendTable> _tableLookup;
		public int ServerClassBits => BitUtils.HighestBitIndex((uint)_dtRef.ServerClasses.Count) + 1; // this might be off for powers of 2
		public readonly PropLookup FlattenedProps; // not initialized during the server class info
		
		
		
		public DataTableParser(SourceDemo demoRef, DataTables dtRef) {
			_demoRef = demoRef;
			_dtRef = dtRef;
			FlattenedProps = new PropLookup();
			// fill up the lookup table so we can quickly reference the tables by name later
			_tableLookup = dtRef.Tables.ToImmutableDictionary(table => table.Name, table => table);
		}
		
		
		public void FlattenClasses() {
			for (int classIndex = 0; classIndex < _dtRef.ServerClasses.Count; classIndex++) {
				ServerClass currentClass = _dtRef.ServerClasses[classIndex];
				
				HashSet<(string, string)> excludes = new HashSet<(string tableName, string propName)>();
				List<ServerClass> baseClasses = new List<ServerClass>();
				SendTable table = _dtRef.Tables[currentClass.DataTableId];

				GatherExcludesAndBaseClasses(excludes, baseClasses, table, true);
				GatherProps(excludes, table, classIndex, "");
				
				List<FlattenedProp> fProps = FlattenedProps[currentClass.DataTableId].flattenedProps;

				// Now we have the props, rearrange them so that props that are marked with 'changes often' get a
				// smaller index. In the new protocol the priority of the props is also taken into account.
				if (_demoRef.DemoSettings.NewDemoProtocol) {
					List<int> priorities = new List<int> {64};
					priorities.AddRange(fProps.Select(entry => entry.Prop.Priority!.Value).Distinct());
					priorities.Sort();
					int start = 0;
					foreach (int priority in priorities) {
						while (true) {
							int currentProp = start;
							while (currentProp < fProps.Count) {
								SendTableProp prop = fProps[currentProp].Prop;
								// ChangesOften gets the same priority as 64
								if (prop.Priority == priority || (DemSet.PropFlagChecker.HasFlag(prop.Flags, ChangesOften) && priority == 64)) {
									if (start != currentProp) {
										FlattenedProp tmp = fProps[start];
										fProps[start] = fProps[currentProp];
										fProps[currentProp] = tmp;
									}
									start++;
									break;
								}
								currentProp++;
							}
							if (currentProp == fProps.Count)
								break;
						}
					}
				} else { // se2007/engine/dt.cpp line 443
					int start = 0;
					while (true) {
						int i;
						for (i = start; i < fProps.Count; i++) {
							FlattenedProp p = fProps[i];
							if (DemSet.PropFlagChecker.HasFlag(p.Prop.Flags, ChangesOften)) {
								fProps[i] = fProps[start];
								fProps[start] = p;
								start++;
								break;
							}
						}
						if (i == fProps.Count)
							break;
					}
				}
			}
			
			// Now that I know the order of the props, I will parse the baselines of any SvcCreateMessages that
			// appeared BEFORE the datatables (valve really do be like that). In game, the baselines are stored as an
			// array and reparsed every time they're updated during demo playback. I just parse them once and store
			// them in a more accessible format.
			
			if (_demoRef.CurStringTablesManager.TableReadable.GetValueOrDefault(TableNames.InstanceBaseLine)) {
				_demoRef.CurStringTablesManager.Tables[TableNames.InstanceBaseLine]
					.Entries.ToList()
					.Select(entry => entry.EntryData)
					.Cast<InstanceBaseline>()
					.ToList()
					.ForEach(baseline => baseline.ParseBaseLineData(FlattenedProps));
			}
		}


		private void GatherExcludesAndBaseClasses(
			ISet<(string, string)> excludes,
			ICollection<ServerClass> baseClasses,
			SendTable table,
			bool collectBaseClasses)
		{
			excludes.UnionWith(
				table.SendProps
					.Where(stp => DemSet.PropFlagChecker.HasFlag(stp.Flags, Exclude))
					.Select(stp => (stp.ExcludeDtName!, stp.Name))
				);
			
			foreach (SendTableProp property in table.SendProps.Where(property => property.SendPropType == SendPropType.DataTable)) {
				if (collectBaseClasses && property.Name == "baseclass") {
					GatherExcludesAndBaseClasses(excludes, baseClasses, _tableLookup[property.ExcludeDtName!], true);
					baseClasses.Add(GetClassByDtName(table.Name)); // should be the same as the properties table
				} else {
					GatherExcludesAndBaseClasses(excludes, baseClasses, _tableLookup[property.ExcludeDtName!], false);
				}
			}
		}


		private void GatherProps(HashSet<(string, string)> excludes, SendTable table, int classIndex, string prefix) {
			List<FlattenedProp> tmpFlattedProps = new List<FlattenedProp>();
			IterateProps(excludes, table, classIndex, tmpFlattedProps, prefix);

			ServerClass classRef = _dtRef.ServerClasses[classIndex];
			int tmpIndex = classRef.DataTableId;
			// in theory the class ID's should always be in consecutive order, although demos seem to imply otherwise
			Debug.Assert(tmpIndex <= FlattenedProps.Count, "server class names are not in consecutive order");
			
			if (tmpIndex == FlattenedProps.Count) // never greater, assert checks for that
				FlattenedProps.Add((classRef, new List<FlattenedProp>()));
			
			FlattenedProps[tmpIndex].flattenedProps.AddRange(tmpFlattedProps);
		}


		private void IterateProps(
			HashSet<(string, string)> excludes, 
			SendTable table,
			int classIndex,
			ICollection<FlattenedProp> fProps,
			string prefix)
		{
			for (int i = 0; i < table.SendProps.Count; i++) {
				SendTableProp prop = table.SendProps[i];
				if (DemSet.PropFlagChecker.HasFlags(prop.Flags, Exclude, InsideArray) || excludes.Contains((table.Name, prop.Name)))
					continue;
				if (prop.SendPropType == SendPropType.DataTable) {
					SendTable subTable = _tableLookup[prop.ExcludeDtName!];
					// we don't prefix Collapsible stuff, since it is just derived mostly
					if (DemSet.PropFlagChecker.HasFlag(prop.Flags, Collapsible))
						IterateProps(excludes, subTable, classIndex, fProps, prefix); 
					else
						GatherProps(excludes, subTable, classIndex, prop.Name.Length > 0 ? $"{prop.Name}." : "");
				} else {
					fProps.Add(new FlattenedProp(DemSet, prefix + prop.Name, prop, 
						prop.SendPropType == SendPropType.Array ? table.SendProps[i - 1] : null));
				}
			}
		}


		public ServerClass GetClassByDtName(string dtName) {
			return _dtRef.ServerClasses.Single(serverClass => serverClass.DataTableName == dtName);
		}
	}
	
	
	public class FlattenedProp {
		
		internal readonly DemoSettings DemoSettings;
		public readonly string Name;
		public readonly SendTableProp Prop;
		public readonly SendTableProp? ArrayElementProp;
		

		public FlattenedProp(DemoSettings demoSettings, string name, SendTableProp prop, SendTableProp? arrayElementProp) {
			Prop = prop;
			ArrayElementProp = arrayElementProp;
			DemoSettings = demoSettings;
			Name = name;
		}
		

		public override string ToString() {
			SendTableProp displayProp = DemoSettings.PropFlagChecker.HasFlag(Prop.Flags, InsideArray) ? ArrayElementProp! : Prop;
			return $"{TypeString()} {Name}, " +
				   $"{displayProp.NumBits} bit{(displayProp.NumBits == 1 ? "" : "s")}, " +
				   $"flags: {displayProp.Flags}";
		}
		
		
		// if elements are of array type, converts from something like "int" to something like "int[32]"
		public string TypeString() {
			return Prop.SendPropType == SendPropType.Array
				? $"{ArrayElementProp?.SendPropType.ToString().ToLower()}[{Prop.NumElements}]" 
				: Prop.SendPropType.ToString().ToLower();
		}
	}


	// typedef todo explicit lookup?
	public class PropLookup : List<(ServerClass serverClass, List<FlattenedProp> flattenedProps)> {} 
}