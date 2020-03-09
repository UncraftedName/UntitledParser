using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.HelperClasses;
using DemoParser.Utils;

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


		internal override void AppendToWriter(IndentedWriter iw) {
			if (ContentsKnown)
				base.AppendToWriter(iw);
			else
				iw += Reader.BitLength > 32
					? $"{Reader.BitLength, 5} bit{(Reader.BitLength > 1 ? "s" : "")}"
					: $"({Reader.ToBinaryString()})";
		}
	}


	public static class StringTableEntryDataFactory {

		public static StringTableEntryData CreateData(SourceDemo demoRef, BitStreamReader bsr, string tableName, string entryName) {
			return tableName switch {
				TableNames.UserInfo 			=> new UserInfo(demoRef, bsr, entryName),
				TableNames.ServerQueryInfo 		=> new QueryPort(demoRef, bsr, entryName),
				TableNames.InstanceBaseLine 	=> new InstanceBaseLine(demoRef, bsr, entryName),
				TableNames.GameRulesCreation 	=> new GameRulesCreation(demoRef, bsr, entryName),
				_ => new StringTableEntryData(demoRef, bsr, entryName)
			};
		}
	}
}