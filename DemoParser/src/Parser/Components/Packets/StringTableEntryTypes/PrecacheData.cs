using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Packets.StringTableEntryTypes {

	public class PrecacheData : StringTableEntryData {

		internal override bool InlineToString => true;
		public PrecacheFlags Flags;


		public PrecacheData(SourceDemo? demoRef, int? decompressedIndex = null) : base(demoRef, decompressedIndex) {}


		protected override void Parse(ref BitStreamReader bsr) {
			Flags = (PrecacheFlags)bsr.ReadUInt(2);
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.Append($"(flags: {Flags})");
		}
	}


	[Flags]
	public enum PrecacheFlags {
		None           = 0,
		FatalIfMissing = 1,      // disconnect if we can't get this file
		Preload        = 1 << 1, // load on client rather than just reserving name
	}
}
