using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Packets.StringTableEntryTypes {
	
	public class QueryPort : StringTableEntryData {
		
		internal override bool InlineToString => true;
		public uint Port;


		public QueryPort(SourceDemo? demoRef) : base(demoRef) {}


		internal override StringTableEntryData CreateCopy() {
			return new QueryPort(DemoRef) {Port = Port};
		}


		protected override void Parse(ref BitStreamReader bsr) {
			Port = bsr.ReadUInt();
		}


		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void AppendToWriter(IIndentedWriter iw) {
			iw.Append(Port.ToString());
		}
	}
}