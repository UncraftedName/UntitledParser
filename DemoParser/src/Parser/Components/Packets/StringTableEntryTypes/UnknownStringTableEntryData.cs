using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Packets.StringTableEntryTypes {
	
	public class UnknownStringTableEntryData : StringTableEntryData {
		
		internal override bool ContentsKnown => false;
		

		public UnknownStringTableEntryData(SourceDemo? demoRef) : base(demoRef) {}


		internal override StringTableEntryData CreateCopy() {
			return new UnknownStringTableEntryData(DemoRef);
		}


		protected override void Parse(ref BitStreamReader bsr) {
			bsr.SkipToEnd();
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void PrettyWrite(IPrettyWriter iw) {
			iw.Append(Reader.BitLength > 64
				? $"{Reader.BitLength, 5} bit{(Reader.BitLength > 1 ? "s" : "")}"
				: $"({Reader.ToBinaryString()})");
		}
	}
}