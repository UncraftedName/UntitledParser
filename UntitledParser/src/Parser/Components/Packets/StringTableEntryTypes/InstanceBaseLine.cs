#nullable enable
using UntitledParser.Parser.Components.Messages;
using UntitledParser.Utils;

namespace UntitledParser.Parser.Components.Packets.StringTableEntryTypes {
	
	// i don't know how to parse this yet, just want a lookup to which class this references
	public class InstanceBaseLine : StringTableEntryData {

		public override bool ContentsKnown => false;

		public ServerClass? ServerClassRef;
		

		public InstanceBaseLine(SourceDemo demoRef, BitStreamReader reader, string entryName) : base(demoRef, reader, entryName) {}


		// todo might just work se2007/engine/sv_packedentities.cpp
		// src_main/engine/networkstringtable.cpp line 1140
		internal override void ParseStream(BitStreamReader bsr) {
			if (DemoRef.DataTableParser != null)
				ServerClassRef = DemoRef.DataTableParser.DataTablesRef.Classes[int.Parse(EntryName)];
		}
		

		internal override void AppendToWriter(IndentedWriter iw) {
			base.AppendToWriter(iw);
			if (ServerClassRef != null)
				iw.Append($"   class: {ServerClassRef.ClassName} ({ServerClassRef.DataTableName})");
		}
	}
}