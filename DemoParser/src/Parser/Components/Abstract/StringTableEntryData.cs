using DemoParser.Parser.Components.Packets.StringTableEntryTypes;
using DemoParser.Parser.HelperClasses;
using DemoParser.Parser.HelperClasses.EntityStuff;

namespace DemoParser.Parser.Components.Abstract {
	
	/// <summary>
	/// Optional data for a string table entry.
	/// </summary>
	public abstract class StringTableEntryData : DemoComponent {
		
		internal virtual bool ContentsKnown => true; // for pretty printing
		internal virtual bool InlineToString => false;
		
		protected StringTableEntryData(SourceDemo? demoRef) : base(demoRef) {}

		internal abstract StringTableEntryData CreateCopy();
	}


	public static class StringTableEntryDataFactory {
		
		// Pass in substream. If prop lookup is populated the baseline will get parsed right away.
		public static StringTableEntryData CreateData(
			SourceDemo? demoRef, string tableName, string entryName,
			PropLookup? propLookup = null)
		{
			return tableName switch {
				TableNames.UserInfo          => new PlayerInfo(demoRef),
				TableNames.ServerQueryInfo   => new QueryPort(demoRef),
				TableNames.InstanceBaseLine  => new InstanceBaseline(demoRef, entryName, propLookup),
				TableNames.GameRulesCreation => new GameRulesCreation(demoRef),
				TableNames.LightStyles       => new LightStyle(demoRef),
				_ => new UnknownStringTableEntryData(demoRef)
			};
		}
	}
}