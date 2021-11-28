using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using ConsoleApp.GenericArgProcessing;
using DemoParser.Parser;
using DemoParser.Parser.Components.Messages;
using DemoParser.Parser.Components.Packets;
using DemoParser.Parser.HelperClasses.EntityStuff;
using DemoParser.Utils;

namespace ConsoleApp.DemoArgProcessing.Options.Hidden {
	
	public class OptDataTablesDump : DemoOption<OptDataTablesDump.DataTableDumpMode> {
		
		public static readonly ImmutableArray<string> DefaultAliases = new[] {"--dump-datatables"}.ToImmutableArray();

		
		public enum DataTableDumpMode {
			PacketAndTree,
			Flattened,
			TreeOnly
		}
		

		public OptDataTablesDump() : base(
			DefaultAliases,
			Arity.ZeroOrOne,
			$"Dumps data table packet(s) and creates a tree of the datable hierarchy {OptOutputFolder.RequiresString}",
			"mode",
			Utils.ParseEnum<DataTableDumpMode>,
			DataTableDumpMode.PacketAndTree,
			true) {}
		
		
		protected override void AfterParse(DemoParsingSetupInfo setupObj, DataTableDumpMode mode, bool isDefault) {
			setupObj.ExecutableOptions++;
			setupObj.FolderOutputRequired = true;
		}


		protected override void Process(DemoParsingInfo infoObj, DataTableDumpMode mode, bool isDefault) {
			try {
				TextWriter tw = infoObj.StartWritingText("creating data table dump", "datatables");
				WriteDataTableDump((StreamWriter)tw, infoObj.CurrentDemo, mode);
			} catch (Exception) {
				Utils.WriteColor("Data table dump failed.\n", ConsoleColor.Red);
			}
		}


		public static void WriteDataTableDump(StreamWriter sw, SourceDemo demo, DataTableDumpMode mode) {
			PrettyStreamWriter pw = new PrettyStreamWriter(sw.BaseStream);
			int datatablesCount = 0; // spliced demos have more than one datatables packet
			foreach (DataTables dataTables in demo.FilterForPacket<DataTables>()) {
				if (datatablesCount++ > 0)
					pw.Append("\n\n\n");
				if (mode != DataTableDumpMode.TreeOnly) {
					switch (mode) {
						case DataTableDumpMode.PacketAndTree:
							dataTables.PrettyWrite(pw);
							break;
						case DataTableDumpMode.Flattened:
							var tableParser = new DataTableParser(demo, dataTables);
							tableParser.FlattenClasses(false);
							foreach ((ServerClass sClass, List<FlattenedProp> fProps) in tableParser.FlattenedProps!) {
								pw.AppendLine($"{sClass.ClassName} ({sClass.DataTableName}) ({fProps.Count} props):");
								for (var i = 0; i < fProps.Count; i++) {
									FlattenedProp fProp = fProps[i];
									pw.Append($"\t({i}): ");
									pw.Append(fProp.TypeString().PadRight(12));
									pw.AppendLine(fProp.ArrayElementPropInfo == null
										? fProp.PropInfo.ToStringNoType()
										: fProp.ArrayElementPropInfo.ToStringNoType());
								}
							}
							break;
						default:
							throw new ArgProcessProgrammerException($"invalid data table dump mode: \"{mode}\"");
					}
				}
				if (mode != DataTableDumpMode.TreeOnly)
					pw.Append("\n\n\n");
				pw.Append("Datatable hierarchy:\n\n\n");
				new DataTableTree(dataTables, true).PrettyWrite(pw);
			}
			// see note at PrettyStreamWriter
			pw.Flush();
		}


		protected override void PostProcess(DemoParsingInfo infoObj, DataTableDumpMode mode, bool isDefault) {}
	}
	
	/// <summary>
	/// Constructs a visualization of the datatable hierarchy. 
	/// </summary>
	public class DataTableTree : PrettyClass {

		private class TableNodeComparer : AlphanumComparator, IComparer<TableNode> {
			public int Compare(TableNode x, TableNode y) => base.Compare(x.Name, y.Name);
		}
		
		
		private readonly ICollection<TableNode> _roots;
		

		public DataTableTree(DataTables dTables, bool sort) {
			
			_roots = sort
				? new SortedSet<TableNode>(new TableNodeComparer())
				: (ICollection<TableNode>)new List<TableNode>();
			
			var namesToAdd = new List<(string name, string? baseName)>(
				dTables.Tables.Select(table => {
					string? baseName = null;
					if (table.SendProps.Count > 0 &&
						table.SendProps[0].SendPropType == SendPropEnums.SendPropType.DataTable)
						baseName = table.SendProps[0].ExcludeDtName!;
					return (table.Name, baseName);
				})
			);
			HashSet<string> addedNames = new HashSet<string>();

			while (namesToAdd.Count > 0) {
				var futureNames = new List<(string, string?)>(namesToAdd.Count);
				foreach ((string name, string? baseName) in namesToAdd) {
					if (baseName == null) {
						_roots.Add(new TableNode(name, sort));
						addedNames.Add(name);
					} else if (addedNames.Contains(baseName)) {
						GetNode(baseName)!.AddChild(name);
						addedNames.Add(name);
					} else {
						futureNames.Add((name, baseName));
					}
				}
				namesToAdd = futureNames;
			}
		}


		private TableNode? GetNode(string name)
			=> _roots.Select(node => node.GetNode(name)).FirstOrDefault(node1 => node1 != null);


		public override void PrettyWrite(IPrettyWriter pw) {
			foreach (TableNode node in _roots)
				node.WritePretty(pw, "", true);
		}


		private class TableNode {
			
			internal readonly string Name;
			private readonly bool _sort;
			private readonly ICollection<TableNode> _children;


			public TableNode(string name, bool sort) {
				Name = name;
				_sort = sort;
				_children = sort
					? new SortedSet<TableNode>(new TableNodeComparer())
					: (ICollection<TableNode>)new List<TableNode>();
			}


			// recursively checks all children for this element, slow but doesn't matter for this
			internal TableNode? GetNode(string name) {
				if (Name == name)
					return this;
				else if (_children.Count == 0)
					return null;
				else
					return _children.Select(node => node.GetNode(name)).FirstOrDefault(node => node != null);
			}


			internal void AddChild(string name) {
				_children.Add(new TableNode(name, _sort));
			}


			internal void WritePretty(IPrettyWriter pw, string indent, bool last) {
				pw.Append(indent);
				if (!last) {
					pw.Append("|-");
					indent += "| ";
				} else {
					pw.Append("\\-");
					indent += "  ";
				}
				pw.AppendLine(Name);
				int i = 0;
				foreach (TableNode child in _children)
					child.WritePretty(pw, indent, ++i == _children.Count);
			}


			public override string ToString() => Name;
		}
	}
}
