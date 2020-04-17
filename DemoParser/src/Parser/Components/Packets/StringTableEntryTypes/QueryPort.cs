using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Packets.StringTableEntryTypes {
	
	public class QueryPort : StringTableEntryData {

		public uint Port;
		
		
		public QueryPort(SourceDemo demoRef, BitStreamReader reader, string entryName) : base(demoRef, reader, entryName) {}
		

		internal override void ParseStream(BitStreamReader bsr) {
			Port = bsr.ReadUInt();
		}


		public override void AppendToWriter(IndentedWriter iw) {
			iw.Append($"port: {Port}");
		}
	}
}