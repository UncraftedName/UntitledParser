using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Packets.StringTableEntryTypes {

	public class QueryPort : StringTableEntryData {

		internal override bool InlineToString => true;
		public uint Port;


		public QueryPort(SourceDemo? demoRef, int? decompressedIndex = null) : base(demoRef, decompressedIndex) {}


		protected override void Parse(ref BitStreamReader bsr) {
			Port = bsr.ReadUInt();
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.Append(Port.ToString());
		}
	}
}
