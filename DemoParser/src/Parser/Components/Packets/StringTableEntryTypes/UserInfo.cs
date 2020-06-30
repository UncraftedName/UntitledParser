using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Packets.StringTableEntryTypes {
	
	// a part of each table entry in the "userinfo" string table
	public class UserInfo : StringTableEntryData {

		// orange box only
		public uint? Version;
		public uint? Xuid;
		
		public string Name;
		public uint UserId;          // local server user ID, unique while server is running
		public string Guid;          // global unique player identifier
		public uint FriendsId;       // friends identification number
		public string FriendsName;
		public bool FakePlayer;      // true, if player is a bot controlled by game.dll
		public bool IsHlTv;          // true if player is the HLTV proxy
		public uint[] CustomFiles;   // custom files CRC for this player
		public uint FilesDownloaded; // this counter increases each time the server downloaded a new file
		
		
		public UserInfo(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}


		internal override void ParseStream(BitStreamReader bsr) {
			if (DemoSettings.NewDemoProtocol) {
				Version = bsr.ReadUInt();
				Xuid = bsr.ReadUInt();
			}
			Name = bsr.ReadNullTerminatedString();

			// todo this depends on the game, also idk what those skipped bytes are
			// probably ready string of length for name and friends name hmmmmmmmmm (csgo uses 128 but that's a bit much)
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
			
			UserId = bsr.ReadUInt();
			Guid = bsr.ReadNullTerminatedString();
			FriendsId = bsr.ReadUInt(); // this might also be different
			FriendsName = bsr.ReadNullTerminatedString();
			FakePlayer = bsr.ReadBool();
			IsHlTv = bsr.ReadBool();
			CustomFiles = new [] {bsr.ReadUInt(), bsr.ReadUInt(), bsr.ReadUInt(), bsr.ReadUInt()};
			FilesDownloaded = bsr.ReadUInt();
		}


		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new System.NotImplementedException();
		}


		public override void AppendToWriter(IndentedWriter iw) {
			iw.AppendLine($"({ParserTextUtils.CamelCaseToUnderscore(GetType().Name)})");
			if (DemoSettings.NewDemoProtocol) {
				iw.AppendLine($"version: {Version}");
				iw.AppendLine($"XUID: {Xuid}");
			}
			iw.AppendLine($"name: {Name}");
			iw.AppendLine($"user ID: {UserId}");
			iw.AppendLine($"GUID: {Guid}");
			iw.AppendLine($"friends ID: {FriendsId}");
			iw.AppendLine($"friends name: {FriendsName}");
			iw.AppendLine($"fake player: {FakePlayer}");
			iw.AppendLine($"is hltv: {IsHlTv}");
			iw.AppendLine($"custom files: {CustomFiles.SequenceToString()}");
			iw.Append($"files downloaded: {FilesDownloaded}");
		}
	}
}