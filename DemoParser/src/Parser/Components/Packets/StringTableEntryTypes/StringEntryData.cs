using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Packets.StringTableEntryTypes {

	// just a string :)
	public class StringEntryData : StringTableEntryData {

		internal override bool InlineToString => true;
		public string Str;

		public StringEntryData(SourceDemo? demoRef, int? decompressedIndex) : base(demoRef, decompressedIndex) {}


		protected override void Parse(ref BitStreamReader bsr) {
			Str = bsr.ReadNullTerminatedString();
		}

		public override void PrettyWrite(IPrettyWriter pw) {
			pw.Append(Str.ToLiteral());
		}
	}
}
