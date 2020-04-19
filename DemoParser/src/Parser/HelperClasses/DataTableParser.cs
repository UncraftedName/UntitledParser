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
		private readonly DataTables _dataTablesRef;
		private readonly ImmutableDictionary<string, SendTable> _tableLookup;
		public int ServerClassBits => BitUtils.HighestBitIndex((uint)_dataTablesRef.ServerClasses.Count) + 1; // this might be off for powers of 2
		public readonly PropLookup? FlattendProps; // not initialized during the server class info
		
		
		
		public DataTableParser(SourceDemo demoRef, DataTables dataTablesRef) {
			_demoRef = demoRef;
			_dataTablesRef = dataTablesRef;
			FlattendProps = new PropLookup();
			// fill up the lookup table so we can quickly reference the tables by name later
			_tableLookup = dataTablesRef.Tables.ToImmutableDictionary(table => table.Name, table => table);
		}
		

		public void FlattenClasses() {
			for (int serverClassIndex = 0; serverClassIndex < _dataTablesRef.ServerClasses.Count; serverClassIndex++) {
				ServerClass currentServerClass = _dataTablesRef.ServerClasses[serverClassIndex];
				
				HashSet<(string, string)> currentExcludes = new HashSet<(string, string)>(); // table name, prop name
				List<ServerClass> currentBaseClasses = new List<ServerClass>();
				SendTable table = _dataTablesRef.Tables[currentServerClass.DataTableId];

				GatherExcludesAndBaseClasses(currentExcludes, currentBaseClasses, table, true);
				GatherProps(currentExcludes, table, serverClassIndex, "");
				
				List<FlattenedProp> flattenedProps = FlattendProps[currentServerClass.DataTableId].flattenedProps;

				// Now we have the props, rearrange them so that props that are marked with 'changes often' get a
				// smaller index. In new engine the priority of the props is also taken into account.
				if (_demoRef.DemoSettings.NewEngine) {
					List<int> priorities = new List<int> {};
					priorities.AddRange(flattenedProps.Select(entry => entry.Property.Priority.Value).Distinct());
					priorities.Sort();
					priorities.Reverse();
					int start = 1;
					foreach (int priority in priorities) {
						while (true) {
							int currentProp = start;
							while (currentProp < flattenedProps.Count) {
								SendTableProperty prop = flattenedProps[currentProp].Property;
								if (prop.Priority.Value == priority || (prop.Flags & SendPropertyFlags.ChangesOften) != 0/* || prop.Name == "m_flAnimTime"*/) {
									if (start != currentProp) {
										FlattenedProp tmp = flattenedProps[start];
										flattenedProps[start] = flattenedProps[currentProp];
										flattenedProps[currentProp] = tmp;
									}
									start++;
									break;
								}
								currentProp++;
							}
							if (currentProp == flattenedProps.Count)
								break;
						}
					}
				} else { // se2007/engine/dt.cpp line 443
					int start = 0;
					while (true) {
						int i;
						for (i = start; i < flattenedProps.Count; i++) {
							FlattenedProp p = flattenedProps[i];
							if ((p.Property.Flags & SendPropertyFlags.ChangesOften) != 0) {
								flattenedProps[i] = flattenedProps[start];
								flattenedProps[start] = p;
								start++;
								break;
							}
						}
						if (i == flattenedProps.Count)
							break;
					}
				}
			}
			
			// now I know how the order of the props, I will go through and update the baselines.
			// (the engine stores the byte array and parses it every time (I think), I do it once so I can call ToString() later)
			
			/*_demoRef.FilterForPacketType<StringTables>().LastOrDefault()?.Tables
				.First(table => table.Name == TableNames.InstanceBaseLine).TableEntries.ToList()
				.ForEach(entry => ((InstanceBaseLine)entry.EntryData).ParseBaseLineData(FlattendProps));*/
			
			if (_demoRef.CStringTablesManager.TableReadable[TableNames.InstanceBaseLine]) {
				_demoRef.CStringTablesManager.Tables[TableNames.InstanceBaseLine]
					.Entries.ToList()
					.ForEach(entry => ((InstanceBaseLine)entry.EntryData).ParseBaseLineData(FlattendProps));
			}
		}


		private void GatherExcludesAndBaseClasses(
			ISet<(string, string)> currentExcludes, 
			ICollection<ServerClass> currentBaseClasses, 
			SendTable table, 
			bool collectBaseClasses) 
		{
			currentExcludes.UnionWith(
				table.Properties
					.Where(property => (property.Flags & SendPropertyFlags.Exclude) != 0)
					.Select(property => (property.ExcludeDtName, property.Name))
				);
			
			foreach (SendTableProperty property in table.Properties.Where(property => property.SendPropertyType == SendPropertyType.DataTable)) {
				if (collectBaseClasses && property.Name == "baseclass") {
					GatherExcludesAndBaseClasses(currentExcludes, currentBaseClasses, _tableLookup[property.ExcludeDtName], true);
					currentBaseClasses.Add(GetClassByDtName(table.Name)); // should be the same as the properties table
				} else {
					GatherExcludesAndBaseClasses(currentExcludes, currentBaseClasses, _tableLookup[property.ExcludeDtName], false);
				}
			}
		}


		private void GatherProps(HashSet<(string, string)> currentExcludes, SendTable table, int serverClassIndex, string prefix) {
			List<FlattenedProp> tmpFlattedProps = new List<FlattenedProp>();
			GatherProps_IterateProps(currentExcludes, table, serverClassIndex, tmpFlattedProps, prefix);

			ServerClass classRef = _dataTablesRef.ServerClasses[serverClassIndex];
			int tmpIndex = classRef.DataTableId;
			// in theory the class ID's should always be in consecutive order, although demos seem to imply otherwise
			Debug.Assert(tmpIndex <= FlattendProps.Count, "server class names are not in consecutive order");
			
			if (tmpIndex == FlattendProps.Count) // never greater, assert checks for that
				FlattendProps.Add((classRef, new List<FlattenedProp>()));
			
			FlattendProps[tmpIndex].flattenedProps.AddRange(tmpFlattedProps);
		}


		private void GatherProps_IterateProps(
			HashSet<(string, string)> currentExcludes, 
			SendTable table, 
			int serverClassIndex, 
			ICollection<FlattenedProp> flattenedProps, 
			string prefix) 
		{
			for (int i = 0; i < table.Properties.Count; i++) {
				SendTableProperty property = table.Properties[i];
				if ((property.Flags & (SendPropertyFlags.InsideArray | SendPropertyFlags.Exclude)) != 0 || currentExcludes.Contains((table.Name, property.Name)))
					continue;
				if (property.SendPropertyType == SendPropertyType.DataTable) {
					SendTable subTable = _tableLookup[property.ExcludeDtName];
					if ((property.Flags & SendPropertyFlags.Collapsible) != 0)
						GatherProps_IterateProps(currentExcludes, subTable, serverClassIndex, flattenedProps, prefix); // we don't prefix Collapsible stuff, since it is just derived mostly
					else
						GatherProps(currentExcludes, subTable, serverClassIndex, property.Name.Length > 0 ? $"{property.Name}." : "");
				} else {
					flattenedProps.Add(new FlattenedProp(prefix + property.Name, property, 
						property.SendPropertyType == SendPropertyType.Array ? table.Properties[i - 1] : null));
				}
			}
		}


		public ServerClass GetClassByDtName(string dtName) {
			return _dataTablesRef.ServerClasses.Single(serverClass => serverClass.DataTableName == dtName);
		}
	}
	
	
	public class FlattenedProp : IEquatable<FlattenedProp> {
		
		public readonly string Name;
		public readonly SendTableProperty Property;
		public readonly SendTableProperty? ArrayElementProp;
		

		public FlattenedProp(string name, SendTableProperty property, SendTableProperty? arrayElementProp) {
			Property = property;
			ArrayElementProp = arrayElementProp;
			Name = name;
		}
		

		public override string ToString() {
			SendTableProperty displayProp = (Property.Flags & SendPropertyFlags.InsideArray) != 0 ? ArrayElementProp : Property;
			return $"{TypeString()} {Name}, " +
				   $"{displayProp.NumBits} bit{(displayProp.NumBits == 1 ? "" : "s")}, " +
				   $"flags: {displayProp.Flags}";
		}
		
		public string TypeString() => TypeStringFromProp(
			Property.SendPropertyType,
			ArrayElementProp?.SendPropertyType,
			Property.Elements);


		private static string TypeStringFromProp(SendPropertyType propertyType, SendPropertyType? elementsType, uint? elements) {
			string str = propertyType switch {
				SendPropertyType.Int 		=> "int",
				SendPropertyType.Float 		=> "float",
				SendPropertyType.Vector3 	=> "vector3",
				SendPropertyType.Vector2 	=> "vector2",
				SendPropertyType.String 	=> "string",
				SendPropertyType.Array 		=> null,
				_ => throw new ArgumentOutOfRangeException(nameof(propertyType), $"unknown property type: {propertyType}")
			};
			// if elements are of array type, converts from something like "int" to something like "int[32]"
			return str ?? $"{TypeStringFromProp(elementsType.Value, default, default)}[{elements}]";
		}


		public bool Equals(FlattenedProp? other) {
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return Name == other.Name && Property.Equals(other.Property) && Equals(ArrayElementProp, other.ArrayElementProp);
		}


		public override bool Equals(object? obj) {
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj.GetType() == GetType() && Equals((FlattenedProp)obj);
		}


		public override int GetHashCode() {
			return HashCode.Combine(Name, Property, ArrayElementProp);
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