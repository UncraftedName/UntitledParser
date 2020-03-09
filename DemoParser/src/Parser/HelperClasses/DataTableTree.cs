using System;
using System.Collections.Generic;
using System.Linq;
using DemoParser.Parser.Components.Packets;
using DemoParser.Utils;

namespace DemoParser.Parser.HelperClasses {
	
	// temporary probably™
	public class DataTableTree {
		
		public List<TableNode> Roots;
		

		public DataTableTree(SourceDemo demo, bool sort) {
			Roots = new List<TableNode>();
			
			var tables = demo.FilterForPacketType<DataTables>()
				.First()
				.Tables;
			
			bool moreToAdd;
			HashSet<string> checkedStrs = new HashSet<string>();
			do {
				moreToAdd = false;
				foreach (SendTable sendTable in tables) {
					if (checkedStrs.Contains(sendTable.Name))
						continue;
					string inheritName = null;
					if (sendTable.Properties.Count > 0 && sendTable.Properties[0].SendPropertyType == SendPropertyType.DataTable)
						inheritName = sendTable.Properties[0].ExcludeDtName;
					if (inheritName == null) {
						AddRoot(sendTable.Name);
						checkedStrs.Add(sendTable.Name);
					} else {
						if (!ContainsElement(inheritName)) {
							moreToAdd = true;
						} else {
							AddElement(sendTable.Name, inheritName);
							checkedStrs.Add(sendTable.Name);
						}
					}
				}
			} while (moreToAdd);

			if (sort) {
				Roots.ForEach(node => node.Sort());
				Roots.Sort((node,  tableNode) => string.Compare(node.Name, tableNode.Name));
			}
		}


		public bool ContainsElement(string name) {
			return Roots.Any(node => node.GetElement(name) != null);
		}


		public void AddRoot(string name) {
			Roots.Add(new TableNode(name, null));
			Roots.Sort((node,  tableNode) => string.Compare(node.Name, tableNode.Name));
		}


		public void AddElement(string name, string parent) {
			Roots.Select(node => node.GetElement(parent)).Single(node => node != null).AddChild(name);
		}


		public void PrintPretty() {
			Roots.ForEach(node => node.PrintPretty("", true));
		}

		
		public class TableNode {
			
			public string Name;
			public List<TableNode> Children;
			public TableNode Parent;
			
			
			public TableNode(string name, TableNode parent) {
				Name = name;
				Parent = parent;
				Children = new List<TableNode>();
			}


			public TableNode GetElement(string name) {
				if (Name == name)
					return this;
				if (Children.Count == 0)
					return null;
				return Children.SingleOrDefault(node => node.GetElement(name) != null);
			}


			public void AddChild(string name) {
				Children.Add(new TableNode(name, this));
			}


			internal void Sort() {
				Children.Sort((node,  tableNode) => string.Compare(node.Name, tableNode.Name));
			}


			public void PrintPretty(string indent, bool last) {
				Console.Write(indent);
				if (!last) {
					Console.Write("|-");
					indent += "| ";
				}
				else {
					Console.Write("\\-");
					indent += "  ";
				}
				Console.WriteLine(Name);
				for (int i = 0; i < Children.Count; i++) 
					Children[i].PrintPretty(indent, i == Children.Count - 1);
			}
		}
	}
}