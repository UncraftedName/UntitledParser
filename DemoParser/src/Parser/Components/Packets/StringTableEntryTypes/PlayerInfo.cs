using DemoParser.Utils;

namespace DemoParser.Parser.Components.Packets.StringTableEntryTypes {
	
	// a part of each table entry in the "userinfo" string table
	public class PlayerInfo : StringTableEntryData {

		// new engine only
		public uint? Version;
		public uint? Xuid;
		
		public string Name;
		public uint UserID;
		public string GUID;
		public uint FriendsID;
		public string FriendsName;
		public bool FakePlayer;
		public bool IsHlTV;
		public uint[] CustomFiles;
		public uint FilesDownloaded;
		
		
		public PlayerInfo(SourceDemo demoRef, BitStreamReader reader, string entryName) : base(demoRef, reader, entryName) {}


		internal override void ParseStream(BitStreamReader bsr) {
			if (DemoRef.DemoSettings.NewEngine) {
				Version = bsr.ReadUInt();
				Xuid = bsr.ReadUInt();
			}
			Name = bsr.ReadNullTerminatedString();
			UserID = bsr.ReadUInt();
			GUID = bsr.ReadNullTerminatedString();
			FriendsID = bsr.ReadUInt();
			FriendsName = bsr.ReadNullTerminatedString();
			FakePlayer = bsr.ReadBool();
			IsHlTV = bsr.ReadBool();
			CustomFiles = new[] {bsr.ReadUInt(), bsr.ReadUInt(), bsr.ReadUInt(), bsr.ReadUInt()};
			FilesDownloaded = bsr.ReadUInt();
		}


		internal override void AppendToWriter(IndentedWriter iw) {
			iw.AppendLine($"({ParserTextUtils.CamelCaseToUnderscore(GetType().Name)})");
			if (DemoRef.DemoSettings.NewEngine) {
				iw.AppendLine($"version: {Version}");
				iw.AppendLine($"XUID: {Xuid}");
			}
			iw.AppendLine($"name: {Name}");
			iw.AppendLine($"user ID: {UserID}");
			iw.AppendLine($"GUID: {GUID}");
			iw.AppendLine($"friends ID: {FriendsID}");
			iw.AppendLine($"friends name: {FriendsName}");
			iw.AppendLine($"fake player: {FakePlayer}");
			iw.AppendLine($"is hltv: {IsHlTV}");
			iw.AppendLine($"custom files: {CustomFiles.SequenceToString()}");
			iw.Append($"files downloaded: {FilesDownloaded}");
		}
	}
}