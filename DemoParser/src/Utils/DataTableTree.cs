using System.Collections.Generic;
using System.Linq;
using DemoParser.Parser.Components.Packets;
using static DemoParser.Parser.HelperClasses.EntityStuff.SendPropEnums;

namespace DemoParser.Utils {
	
	/// <summary>
	/// Constructs a visualizable tree of the server class hierarchy. 
	/// </summary>
	public class DataTableTree {
		private readonly List<TableNode> _roots;
		

		public DataTableTree(DataTables dTables, bool sort) {
			
			_roots = new List<TableNode>();
			
			bool moreToAdd;
			HashSet<string> checkedStrs = new HashSet<string>();
			do {
				moreToAdd = false;
				foreach (SendTable sendTable in dTables.Tables) {
					if (checkedStrs.Contains(sendTable.Name))
						continue;
					string? inheritName = null;
					if (sendTable.SendProps.Count > 0 && sendTable.SendProps[0].SendPropType == SendPropType.DataTable)
						inheritName = sendTable.SendProps[0].ExcludeDtName!;
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
				_roots.ForEach(node => node.Sort());
				_roots.Sort((node, tableNode) => string.CompareOrdinal(node.Name, tableNode.Name));
			}
		}


		private bool ContainsElement(string name) {
			return _roots.Any(node => node.GetElement(name) != null);
		}


		private void AddRoot(string name) {
			_roots.Add(new TableNode(name));
			_roots.Sort((node,  tableNode) => string.CompareOrdinal(node.Name, tableNode.Name));
		}


		private void AddElement(string name, string parent) {
			_roots.Select(node => node.GetElement(parent)).Single(node => node != null).AddChild(name);
		}


		public override string ToString() {
			IIndentedWriter iw = new IndentedToStringWriter();
			_roots.ForEach(node => node.WritePretty(iw, "", true));
			return iw.ToString();
		}


		internal class TableNode {
			
			internal readonly string Name;
			private readonly List<TableNode> _children;


			internal TableNode(string name) {
				Name = name;
				_children = new List<TableNode>();
			}


			internal TableNode? GetElement(string name) {
				if (Name == name)
					return this;
				else if (_children.Count == 0)
					return null;
				else 
					return _children.SingleOrDefault(node => node.GetElement(name) != null);
			}


			internal void AddChild(string name) {
				_children.Add(new TableNode(name));
			}


			internal void Sort() {
				_children.Sort((node,  tableNode) => string.CompareOrdinal(node.Name, tableNode.Name));
			}


			internal void WritePretty(IIndentedWriter iw, string indent, bool last) {
				iw.Append(indent);
				if (!last) {
					iw.Append("|-");
					indent += "| ";
				}
				else {
					iw.Append("\\-");
					indent += "  ";
				}
				iw.AppendLine(Name);
				for (int i = 0; i < _children.Count; i++) 
					_children[i].WritePretty(iw, indent, i == _children.Count - 1);
			}
		}
	}
}