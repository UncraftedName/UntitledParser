using System;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Packets.StringTableEntryTypes {
	
	// a part of each table entry in the "userinfo" string table
	public class UserInfo : StringTableEntryData {

		// new engine only
		public uint? Version;
		public uint? Xuid;
		
		public string Name;
		public uint UserID; // local server user ID, unique while server is running
		public string GUID; // global unique player identifer
		public uint FriendsID; // friends identification number
		public string FriendsName;
		public bool FakePlayer; // true, if player is a bot controlled by game.dll
		public bool IsHlTV; // true if player is the HLTV proxy
		public uint[] CustomFiles; // custom files CRC for this player
		public uint FilesDownloaded; // this counter increases each time the server downloaded a new file
		
		
		public UserInfo(SourceDemo demoRef, BitStreamReader reader, string entryName) : base(demoRef, reader, entryName) {}


		internal override void ParseStream(BitStreamReader bsr) {
			if (DemoRef.DemoSettings.NewEngine) {
				Version = bsr.ReadUInt();
				Xuid = bsr.ReadUInt();
			}
			Name = bsr.ReadNullTerminatedString();

			// todo this depends on the game, also idk what those skipped bytes are
			/*switch (DemoRef.DemoSettings.Game) {
				case SourceDemoSettings.SourceGame.PORTAL_2:
					Console.WriteLine(bsr.SubStream(8 * 22).ToHexString());
					bsr.SkipBytes(18);
					break;
				case SourceDemoSettings.SourceGame.L4D2_2000:
					bsr.SkipBytes(19);
					break;
				default:
					bsr.SkipBytes(18);
					break;
			}*/
			bsr.SkipBytes(18);
			
			UserID = bsr.ReadUInt();
			GUID = bsr.ReadNullTerminatedString();
			FriendsID = bsr.ReadUInt(); // this might also be different
			FriendsName = bsr.ReadNullTerminatedString();
			FakePlayer = bsr.ReadBool();
			IsHlTV = bsr.ReadBool();
			CustomFiles = new [] {bsr.ReadUInt(), bsr.ReadUInt(), bsr.ReadUInt(), bsr.ReadUInt()};
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