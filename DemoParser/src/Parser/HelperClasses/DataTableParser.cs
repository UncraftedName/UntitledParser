#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using DemoParser.Parser.Components.Messages;
using DemoParser.Parser.Components.Packets;
using DemoParser.Parser.Components.Packets.StringTableEntryTypes;
using DemoParser.Utils;

namespace DemoParser.Parser.HelperClasses {
	
	// https://github.com/StatsHelix/demoinfo/blob/ac3e820d68a5a76b1c4c86bf3951e9799f669a56/DemoInfo/DT/DataTableParser.cs#L69
	public class DataTableParser {

		private readonly SourceDemo _demoRef;
		private readonly DataTables _dtRef;
		private readonly ImmutableDictionary<string, SendTable> _tableLookup;
		public int ServerClassBits => BitUtils.HighestBitIndex((uint)_dtRef.ServerClasses.Count) + 1; // this might be off for powers of 2
		public readonly PropLookup? FlattenedProps; // not initialized during the server class info
		
		
		
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
				
				HashSet<(string, string)> excludes = new HashSet<(string, string)>(); // table name, prop name
				List<ServerClass> baseClasses = new List<ServerClass>();
				SendTable table = _dtRef.Tables[currentClass.DataTableId];

				GatherExcludesAndBaseClasses(excludes, baseClasses, table, true);
				GatherProps(excludes, table, classIndex, "");
				
				List<FlattenedProp> fProps = FlattenedProps[currentClass.DataTableId].flattenedProps;

				// Now we have the props, rearrange them so that props that are marked with 'changes often' get a
				// smaller index. In orange box the priority of the props is also taken into account.
				if (_demoRef.DemoSettings.OrangeBox) {
					List<int> priorities = new List<int>(); // csgo parser inits this with 64, literally no clue why
					priorities.AddRange(fProps.Select(entry => entry.Prop.Priority.Value).Distinct());
					priorities.Sort();
					// csgo parser doesn't reverse here, portal 2 seems to require it
					priorities.Reverse();
					// So this might actually start at 0, but that kinda breaks for portal 2 demos for 
					// flAnimTimeMustBeFirst or whatever, and starting at 1 doesn't seem to effect portal 1 demos.
					int start = 1;
					foreach (int priority in priorities) {
						while (true) {
							int currentProp = start;
							while (currentProp < fProps.Count) {
								SendTableProp prop = fProps[currentProp].Prop;
								// in csgo parser, check for ChangesOften is accompanied by a check for priority a of 64 
								if (prop.Priority.Value == priority || (prop.Flags & SendPropFlags.ChangesOften) != 0) {
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
							if ((p.Prop.Flags & SendPropFlags.ChangesOften) != 0) {
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

			if (_demoRef.CStringTablesManager.TableReadable.GetValueOrDefault(TableNames.InstanceBaseLine)) {
				_demoRef.CStringTablesManager.Tables[TableNames.InstanceBaseLine]
					.Entries.ToList()
					.ForEach(entry => ((InstanceBaseline)entry.EntryData).ParseBaseLineData(FlattenedProps));
			}
		}


		private void GatherExcludesAndBaseClasses(
			ISet<(string, string)> excludes, 
			ICollection<ServerClass> baseClasses, 
			SendTable table, 
			bool collectBaseClasses) 
		{
			excludes.UnionWith(
				table.Properties
					.Where(property => (property.Flags & SendPropFlags.Exclude) != 0)
					.Select(property => (property.ExcludeDtName, property.Name))
				);
			
			foreach (SendTableProp property in table.Properties.Where(property => property.SendPropType == SendPropType.DataTable)) {
				if (collectBaseClasses && property.Name == "baseclass") {
					GatherExcludesAndBaseClasses(excludes, baseClasses, _tableLookup[property.ExcludeDtName], true);
					baseClasses.Add(GetClassByDtName(table.Name)); // should be the same as the properties table
				} else {
					GatherExcludesAndBaseClasses(excludes, baseClasses, _tableLookup[property.ExcludeDtName], false);
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
			for (int i = 0; i < table.Properties.Count; i++) {
				SendTableProp prop = table.Properties[i];
				if ((prop.Flags & (SendPropFlags.InsideArray | SendPropFlags.Exclude)) != 0 || excludes.Contains((table.Name, prop.Name)))
					continue;
				if (prop.SendPropType == SendPropType.DataTable) {
					SendTable subTable = _tableLookup[prop.ExcludeDtName];
					// we don't prefix Collapsible stuff, since it is just derived mostly
					if ((prop.Flags & SendPropFlags.Collapsible) != 0)
						IterateProps(excludes, subTable, classIndex, fProps, prefix); 
					else
						GatherProps(excludes, subTable, classIndex, prop.Name.Length > 0 ? $"{prop.Name}." : "");
				} else {
					fProps.Add(new FlattenedProp(prefix + prop.Name, prop, 
						prop.SendPropType == SendPropType.Array ? table.Properties[i - 1] : null));
				}
			}
		}


		public ServerClass GetClassByDtName(string dtName) {
			return _dtRef.ServerClasses.Single(serverClass => serverClass.DataTableName == dtName);
		}
	}
	
	
	public class FlattenedProp : IEquatable<FlattenedProp> {
		
		public readonly string Name;
		public readonly SendTableProp Prop;
		public readonly SendTableProp? ArrayElementProp;
		

		public FlattenedProp(string name, SendTableProp prop, SendTableProp? arrayElementProp) {
			Prop = prop;
			ArrayElementProp = arrayElementProp;
			Name = name;
		}
		

		public override string ToString() {
			SendTableProp displayProp = (Prop.Flags & SendPropFlags.InsideArray) != 0 ? ArrayElementProp : Prop;
			return $"{TypeString()} {Name}, " +
				   $"{displayProp.NumBits} bit{(displayProp.NumBits == 1 ? "" : "s")}, " +
				   $"flags: {displayProp.Flags}";
		}
		
		
		// if elements are of array type, converts from something like "int" to something like "int[32]"
		public string TypeString() {
			return Prop.SendPropType == SendPropType.Array
				? $"{ArrayElementProp?.SendPropType.ToString().ToLower()}[{Prop.Elements}]" 
				: Prop.SendPropType.ToString().ToLower();
		}


		public bool Equals(FlattenedProp? other) {
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return Name == other.Name && Prop.Equals(other.Prop) && Equals(ArrayElementProp, other.ArrayElementProp);
		}


		public override bool Equals(object? obj) {
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj.GetType() == GetType() && Equals((FlattenedProp)obj);
		}


		public override int GetHashCode() {
			return HashCode.Combine(Name, Prop, ArrayElementProp);
		}


		public static bool operator ==(FlattenedProp left, FlattenedProp right) {
			return Equals(left, right);
		}


		public static bool operator !=(FlattenedProp left, FlattenedProp right) {
			return !Equals(left, right);
		}
	}


	// typedef
	public class PropLookup : List<(ServerClass serverClass, List<FlattenedProp> flattenedProps)> {} 
}