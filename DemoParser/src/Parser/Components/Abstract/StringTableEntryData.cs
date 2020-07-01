using DemoParser.Parser.Components.Packets.StringTableEntryTypes;
using DemoParser.Parser.HelperClasses;
using DemoParser.Parser.HelperClasses.EntityStuff;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Abstract {
	
	/// <summary>
	/// Optional data for a string table entry.
	/// </summary>
	public abstract class StringTableEntryData : DemoComponent {
		
		internal virtual bool ContentsKnown => true; // for pretty printing
		
		protected StringTableEntryData(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
	}


	public static class StringTableEntryDataFactory {
		
		// Pass in substream. If prop lookup is populated the baseline will get parsed right away.
		public static StringTableEntryData CreateData(
			SourceDemo demoRef, BitStreamReader bsr, string tableName, string entryName, // mandatory stuff
			PropLookup propLookup = null) // if parsing baseline now
		{
			return tableName switch {
				TableNames.UserInfo          => new UserInfo(demoRef, bsr),
				TableNames.ServerQueryInfo   => new QueryPort(demoRef, bsr),
				TableNames.InstanceBaseLine  => new InstanceBaseline(demoRef, bsr, entryName, propLookup),
				TableNames.GameRulesCreation => new GameRulesCreation(demoRef, bsr),
				_ => new UnknownStringTableEntryData(demoRef, bsr)
			};
		}
	}
}