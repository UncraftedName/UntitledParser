using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.HelperClasses;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Packets.StringTableEntryTypes {

	// Sometimes the contents are known and sometimes they aren't.
	// Serves a similar purpose to UnknownSvcUserMessage.
	public class StringTableEntryData : DemoComponent {

		public sealed override bool MayContainData => true;
		public virtual bool ContentsKnown => GetType().IsSubclassOf(typeof(StringTableEntryData));
		protected readonly string EntryName;
		

		public StringTableEntryData(SourceDemo demoRef, BitStreamReader reader, string entryName) : base(demoRef, reader) {
			EntryName = entryName;
		}
		
		
		internal override void ParseStream(BitStreamReader bsr) {}


		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void AppendToWriter(IndentedWriter iw) {
			if (ContentsKnown)
				base.AppendToWriter(iw);
			else
				iw.Append(Reader.BitLength > 32
					? $"{Reader.BitLength, 5} bit{(Reader.BitLength > 1 ? "s" : "")}"
					: $"({Reader.ToBinaryString()})");
		}
	}


	public static class StringTableEntryDataFactory {

		// if passing the server class, then the baseline gets parsed right away
		public static StringTableEntryData CreateData(
			SourceDemo demoRef, BitStreamReader bsr, string tableName, string entryName, // mandatory stuff
			PropLookup propLookup = null) // if parsing baseline now
		{
			return tableName switch {
				TableNames.UserInfo 			=> new UserInfo(demoRef, bsr, entryName),
				TableNames.ServerQueryInfo 		=> new QueryPort(demoRef, bsr, entryName),
				TableNames.InstanceBaseLine 	=> new InstanceBaseLine(demoRef, bsr, entryName, propLookup),
				TableNames.GameRulesCreation 	=> new GameRulesCreation(demoRef, bsr, entryName),
				_ => new StringTableEntryData(demoRef, bsr, entryName)
			};
		}
	}
}