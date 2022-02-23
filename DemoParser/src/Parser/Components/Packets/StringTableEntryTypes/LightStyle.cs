using System.Linq;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Packets.StringTableEntryTypes {

	public class LightStyle : StringTableEntryData {

		internal override bool InlineToString => true;
		public byte[]? Values;


		public LightStyle(SourceDemo? demoRef, int? decompressedIndex = null) : base(demoRef, decompressedIndex) {}


		protected override void Parse(ref BitStreamReader bsr) {
			string str = bsr.ReadNullTerminatedString(); // yes
			if (str.Length != 0)
				Values = str.ToCharArray().Select(c => (byte)((c - 'a') * 22)).ToArray();
		}


		// see src_main/engine/gl_rlight.cpp
		public override void PrettyWrite(IPrettyWriter pw) {
			pw.Append(Values == null
				? "0 frames (256)"
				: $"{Values.Length} frame{(Values.Length > 1 ? "s" : "")}: {Values.SequenceToString().Replace(",", "")}");
		}
	}
}
