using DemoParser.Parser.Components.Packets.StringTableEntryTypes;
using DemoParser.Parser.GameState;

namespace DemoParser.Parser.Components.Abstract {

	/// <summary>
	/// Optional data for a string table entry.
	/// </summary>
	public abstract class StringTableEntryData : DemoComponent {

		// for pretty printing
		internal virtual bool ContentsKnown => true;
		internal virtual bool InlineToString => false;

		protected StringTableEntryData(SourceDemo? demoRef, int? decompressedIndex) : base(demoRef, decompressedIndex) {}
	}


	public static class StringTableEntryDataFactory {
		// entry data may come from a compressed stream, so provide an index into the decompressed lookup table
		public static StringTableEntryData CreateEntryData(SourceDemo? dRef, int? decompressedIndex, string tableName, string entryName) {
			return tableName switch {
				TableNames.UserInfo          => new PlayerInfo      (dRef, decompressedIndex),
				TableNames.ServerQueryInfo   => new QueryPort       (dRef, decompressedIndex),
				TableNames.InstanceBaseLine  => new InstanceBaseline(dRef, entryName, decompressedIndex),
				TableNames.GameRulesCreation => new StringEntryData (dRef, decompressedIndex),
				TableNames.InfoPanel         => new StringEntryData (dRef, decompressedIndex),
				TableNames.LightStyles       => new LightStyle      (dRef, decompressedIndex),
				TableNames.ModelPreCache     => new PrecacheData    (dRef, decompressedIndex),
				TableNames.GenericPreCache   => new PrecacheData    (dRef, decompressedIndex),
				TableNames.SoundPreCache     => new PrecacheData    (dRef, decompressedIndex),
				TableNames.DecalPreCache     => new PrecacheData    (dRef, decompressedIndex),
				_                 => new UnknownStringTableEntryData(dRef, decompressedIndex)
			};
		}
	}
}
