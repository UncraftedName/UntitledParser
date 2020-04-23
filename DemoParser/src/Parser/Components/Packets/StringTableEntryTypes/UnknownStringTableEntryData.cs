using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Packets.StringTableEntryTypes {
	
	public class UnknownStringTableEntryData : StringTableEntryData {
		
		internal override bool ContentsKnown => false;
		

		public UnknownStringTableEntryData(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new System.NotImplementedException();
		}


		public override void AppendToWriter(IndentedWriter iw) {
			iw.Append(Reader.BitLength > 64
				? $"{Reader.BitLength, 5} bit{(Reader.BitLength > 1 ? "s" : "")}"
				: $"({Reader.ToBinaryString()})");
		}
	}
}