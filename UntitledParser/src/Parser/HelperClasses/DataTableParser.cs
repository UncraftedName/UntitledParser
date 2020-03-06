using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UntitledParser.Parser.Components.Messages;
using UntitledParser.Parser.Components.Packets;
using UntitledParser.Utils;

namespace UntitledParser.Parser.HelperClasses {
	// https://github.com/StatsHelix/demoinfo/blob/ac3e820d68a5a76b1c4c86bf3951e9799f669a56/DemoInfo/DT/DataTableParser.cs#L69
	public class DataTableParser {

		private SourceDemo DemoRef;
		public DataTables DataTablesRef;
		public int ClassBits => BitUtils.HighestBitIndex((uint)DataTablesRef.Classes.Count) + 1; // this might be off for powers of 2
		// get { return (int)Math.Ceiling(Math.Log(ServerClasses.Count, 2)); } 
		public Dictionary<ServerClass, List<ServerClass>> BaseClassLookup;
		public Dictionary<ServerClass, List<FlattenedPropEntry>> FlattendPropLookup;
		
		
		
		public DataTableParser(SourceDemo demoRef, DataTables dataTablesRef) {
			DemoRef = demoRef;
			DataTablesRef = dataTablesRef;
		}
		

		[SuppressMessage("ReSharper", "PossibleInvalidOperationException")]
		public void FlattenClasses() {
			BaseClassLookup = new Dictionary<ServerClass, List<ServerClass>>();
			FlattendPropLookup = new Dictionary<ServerClass, List<FlattenedPropEntry>>();

			for (int serverClassIndex = 0; serverClassIndex < DataTablesRef.Classes.Count; serverClassIndex++) {
				ServerClass currentServerClass = DataTablesRef.Classes[serverClassIndex];
				
				List<ExcludeEntry> currentExcludes = new List<ExcludeEntry>();
				List<ServerClass> currentBaseClasses = new List<ServerClass>();
				SendTable table = DataTablesRef.Tables[(int)currentServerClass.DataTableID]; // this might be equivalent to [i]

				GatherExcludesAndBaseClasses(currentExcludes, currentBaseClasses, table, true);
				BaseClassLookup[currentServerClass] = currentBaseClasses;
				GatherProps(table, serverClassIndex, "");
				
				List<FlattenedPropEntry> flattenedProps = FlattendPropLookup[currentServerClass];

				if (DemoRef.DemoSettings.NewEngine) {

					List<int> priorities = new List<int> {64};
					priorities.AddRange(flattenedProps.Select(entry => entry.Property.Priority.Value).Distinct());
					priorities.Sort();
					int start = 0;
					foreach (int priority in priorities) {
						while (true) {
							int currentProp = start;
							while (currentProp < flattenedProps.Count) {
								SendTableProperty prop = flattenedProps[currentProp].Property;
								if (prop.Priority.Value == priority
									|| priority == 64 && (prop.Flags & SendPropertyFlags.ChangesOften) != 0) 
								{
									if (start != currentProp) {
										FlattenedPropEntry tmp = flattenedProps[start];
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
					int i, start = 0;
					while (true) {
						for (i = start; i < flattenedProps.Count; i++) {
							FlattenedPropEntry p = flattenedProps[i];
							// unsigned char c = bhs->m_PropProxyIndices[i];
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
		}


		public void GatherExcludesAndBaseClasses(
			List<ExcludeEntry> currentExcludes, 
			List<ServerClass> currentBaseClasses, 
			SendTable table, 
			bool collectBaseClasses) 
		{
			currentExcludes.AddRange(
				table.Properties
					.Where(property => (property.Flags & SendPropertyFlags.Exclude) != 0)
					.Select(property => new ExcludeEntry(property.Name, property.TableRef.Name, table.Name))
				);
			
			foreach (SendTableProperty property in table.Properties.Where(property => property.SendPropertyType == SendPropertyType.DataTable)) {
				if (collectBaseClasses && property.Name == "baseclass") {
					GatherExcludesAndBaseClasses(currentExcludes, currentBaseClasses, GetTableByName(property.ExcludeDtName), true);
					currentBaseClasses.Add(GetClassByDTName(property.TableRef.Name));
				} else {
					GatherExcludesAndBaseClasses(currentExcludes, currentBaseClasses, GetTableByName(property.ExcludeDtName), false);
				}
			}
		}


		public void GatherProps(SendTable table, int serverClassIndex, string prefix) {
			List<FlattenedPropEntry> tmpFlattedProps = new List<FlattenedPropEntry>();
			GatherProps_IterateProps(table, serverClassIndex, tmpFlattedProps, prefix);

			ServerClass classRef = DataTablesRef.Classes[serverClassIndex];
			if (!FlattendPropLookup.ContainsKey(classRef))
				FlattendPropLookup[classRef] = new List<FlattenedPropEntry>();
			FlattendPropLookup[classRef].AddRange(tmpFlattedProps);
		}


		public void GatherProps_IterateProps(SendTable table, int serverClassIndex, List<FlattenedPropEntry> flattenedProps, string prefix) {
			for (int i = 0; i < table.Properties.Count; i++) {
				SendTableProperty property = table.Properties[i];
				if ((property.Flags & (SendPropertyFlags.InsideArray | SendPropertyFlags.Exclude)) != 0)
					continue;
				if (property.SendPropertyType == SendPropertyType.DataTable) {
					SendTable subTable = GetTableByName(property.ExcludeDtName);
					if ((property.Flags & SendPropertyFlags.Collapsible) != 0)
						GatherProps_IterateProps(subTable, serverClassIndex, flattenedProps, prefix); // we don't prefix Collapsible stuff, since it is just derived mostly
					else
						GatherProps(subTable, serverClassIndex, property.Name.Length > 0 ? $"{property.Name}." : "");
				} else {
					flattenedProps.Add(new FlattenedPropEntry(prefix + property.Name, property, 
						property.SendPropertyType == SendPropertyType.Array ? table.Properties[i - 1] : null));
				}
			}
		}


		public SendTable GetTableByName(string name) {
			return DataTablesRef.Tables.Single(table => table.Name == name);
		}


		public ServerClass GetClassByDTName(string dtName) {
			return DataTablesRef.Classes.Single(serverClass => serverClass.DataTableName == dtName);
		}


		public SendTable GetTableByIndex(int i) {
			return DataTablesRef.Tables[i];
		}
	}
	
	
	public class ExcludeEntry  {

		public string VarName;
		public string DTName;
		public string ExcludingDT;
		
		
		public ExcludeEntry( string varName, string dtName, string excludingDT ) {
			VarName = varName;
			DTName = dtName;
			ExcludingDT = excludingDT;
		}
	}
	
	
	public class FlattenedPropEntry {
		
		public SendTableProperty Property;
		public SendTableProperty ArrayElementProp;
		public string Name;
		

		public FlattenedPropEntry(string name, SendTableProperty property, SendTableProperty arrayElementProp) {
			Property = property;
			ArrayElementProp = arrayElementProp;
			Name = name;
		}

		public override string ToString() {
			return string.Format("[FlattenedPropEntry: PropertyName={2}, Prop={0}, ArrayElementProp={1}]", Property, ArrayElementProp, Name);
		}
	}
}