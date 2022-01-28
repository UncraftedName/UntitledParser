using DemoParser.Parser.Components.Packets.StringTableEntryTypes;
using DemoParser.Parser.HelperClasses;
using DemoParser.Parser.HelperClasses.EntityStuff;
using DemoParser.Parser.HelperClasses.GameState;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Abstract {

	/// <summary>
	/// Optional data for a string table entry.
	/// </summary>
	public abstract class StringTableEntryData : DemoComponent {

		// store the index to the decompressed data in the lookup table if we access the reader later, might make a special class for this
		protected readonly int? DecompressedIndex;

		public override BitStreamReader Reader =>
			DecompressedIndex.HasValue
				? new BitStreamReader(DemoRef.DecompressedLookup[DecompressedIndex.Value], AbsoluteBitEnd - AbsoluteBitStart, AbsoluteBitStart)
				: base.Reader;

		internal virtual bool ContentsKnown => true; // for pretty printing
		internal virtual bool InlineToString => false;

		protected StringTableEntryData(SourceDemo? demoRef, int? decompressedIndex) : base(demoRef) {
			DecompressedIndex = decompressedIndex;
		}

		internal abstract StringTableEntryData CreateCopy();
	}


	public static class StringTableEntryDataFactory {
		public static StringTableEntryData CreateEntryData(SourceDemo? demoRef, int? decompressedIndex, string tableName, string entryName) {
			return tableName switch {
				TableNames.UserInfo          => new PlayerInfo(demoRef, decompressedIndex),
				TableNames.ServerQueryInfo   => new QueryPort(demoRef, decompressedIndex),
				TableNames.InstanceBaseLine  => new InstanceBaseline(demoRef, decompressedIndex, entryName),
				TableNames.GameRulesCreation => new GameRulesCreation(demoRef, decompressedIndex),
				TableNames.LightStyles       => new LightStyle(demoRef, decompressedIndex),
				_ => new UnknownStringTableEntryData(demoRef, decompressedIndex)
			};
		}
	}
}
