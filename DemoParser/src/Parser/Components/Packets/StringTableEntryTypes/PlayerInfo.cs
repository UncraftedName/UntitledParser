using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;
using static DemoParser.Parser.Components.Packets.StringTableEntryTypes.PlayerInfo.CSteamId.EAccountType;
using static DemoParser.Parser.Components.Packets.StringTableEntryTypes.PlayerInfo.CSteamId.EChatSteamIDInstanceFlags;

namespace DemoParser.Parser.Components.Packets.StringTableEntryTypes {
	
	// table entry in the "userinfo" string table
	public class PlayerInfo : StringTableEntryData {
		
		public CSteamId? SteamId; // new
		public CharArray Name;       // scoreboard information
		public int UserId;           // local server user ID, unique while server is running
		public CharArray Guid;       // global unique player identifier
		public uint FriendsId;       // friends identification number
		public CharArray FriendsName;
		public bool FakePlayer;      // true, if player is a bot controlled by game.dll
		public bool IsHlTv;          // true if player is the HLTV proxy
		public uint[] CustomFiles;   // custom files CRC for this player
		public byte FilesDownloaded; // this counter increases each time the server downloaded a new file
		
		
		public PlayerInfo(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}


		// since we're reading a struct, alignment needs to be taken into account
		internal override void ParseStream(BitStreamReader bsr) {
			if (DemoSettings.NewDemoProtocol)
				SteamId = (CSteamId)bsr.ReadULong();
			Name = bsr.ReadCharArray(DemoSettings.MaxPlayerNameLength);
			UserId = bsr.ReadSInt();
			Guid = bsr.ReadCharArray(DemoSettings.SignedGuidLen + 1);
			bsr.SkipBytes(3);
			FriendsId = bsr.ReadUInt();
			FriendsName = bsr.ReadCharArray(DemoSettings.MaxPlayerNameLength);
			FakePlayer = bsr.ReadByte() != 0;
			IsHlTv = bsr.ReadByte() != 0;
			CustomFiles = new[] {bsr.ReadUInt(), bsr.ReadUInt(), bsr.ReadUInt(), bsr.ReadUInt()};
			FilesDownloaded = bsr.ReadByte();
			bsr.SkipBytes(3);
			// for demo protocol 4 there's 4 additional bytes somewhere for some reason
		}


		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void AppendToWriter(IndentedWriter iw) {
			if (DemoSettings.NewDemoProtocol)
				iw.AppendLine($"steam ID: {SteamId.ToString()}");
			iw.AppendLine($"name: {Name}");
			iw.AppendLine($"user ID: {UserId}");
			iw.AppendLine($"GUID: {Guid}");
			iw.AppendLine($"friends ID: {FriendsId}");
			iw.AppendLine($"friends name: {FriendsName}");
			iw.AppendLine($"fake player: {FakePlayer}");
			iw.AppendLine($"is hltv: {IsHlTv}");
			iw.AppendLine($"custom file crc's: [logo: 0x{CustomFiles[0]:X}, sounds: 0x{CustomFiles[1]:X}, " +
						  $"models: 0x{CustomFiles[2]:X}, txt: 0x{CustomFiles[3]:X}]");
			iw.Append($"files downloaded: {FilesDownloaded}");
		}


		[SuppressMessage("ReSharper", "InconsistentNaming")]
		[SuppressMessage("ReSharper", "IdentifierTypo")]
		[StructLayout(LayoutKind.Explicit, Size = 8)]
		public struct CSteamId {
			
			[field: FieldOffset(0)]
			public ulong m_unAll64Bits {get;set;}

			public static implicit operator ulong(CSteamId c) => c.m_unAll64Bits;
			public static explicit operator CSteamId(ulong u) => new CSteamId(u);

			public CSteamId(ulong u) => m_unAll64Bits = u;

			public uint m_unAccountId {
				readonly get => (uint)m_unAll64Bits;
				set => m_unAll64Bits = (m_unAll64Bits & 0xFFFFFFFF00000000) | value;
			}
			public uint m_unAccountInstance {
				readonly get => (uint)(m_unAll64Bits >> 32) & 0x000FFFFF;
				set => m_unAll64Bits = (m_unAll64Bits & 0xFFF00000FFFFFFFF) | ((ulong)(value & 0x000FFFFF) << 32);
			}
			public EAccountType m_EAccountType {
				readonly get => (EAccountType)((m_unAll64Bits >> 52) & 0xF);
				set => m_unAll64Bits = (m_unAll64Bits & 0xFF0FFFFFFFFFFFFF) | (((ulong)value & 0xF) << 52);
			}
			public EUniverse m_EUniverse {
				readonly get => (EUniverse)((m_unAll64Bits >> 56) & 0xFF);
				set => m_unAll64Bits = (m_unAll64Bits & 0xFFFFFFFFFFFFFF) | ((ulong)value & 0xFF) << 56;
			}


			public readonly ulong StaticAccountKey 
				=> ((ulong)m_EUniverse << 56) + ((ulong)m_EAccountType << 52) + m_unAccountId;

			public readonly bool IsAnonAccount
				=> m_EAccountType == k_EAccountTypeAnonUser || m_EAccountType == k_EAccountTypeAnonGameServer;

			public readonly bool IsBlankAnonymousAccount
				=> m_unAccountId == 0 && m_unAccountInstance == 0 && IsAnonAccount;

			public readonly bool IsGameServerAccount
				=> m_EAccountType == k_EAccountTypeGameServer || m_EAccountType == k_EAccountTypeAnonGameServer;

			public readonly bool IsLobby // chat account ID
				=> m_EAccountType == k_EAccountTypeChat && (m_unAccountInstance & (ulong)k_EChatInstanceFlagLobby) != 0;

			public readonly bool IsValid {
				get {
					if (m_EAccountType <= k_EAccountTypeInvalid || m_EAccountType >= k_EAccountTypeMax)
						return false;
					if (m_EUniverse <= EUniverse.k_EUniverseInvalid || m_EUniverse >= EUniverse.k_EUniverseMax)
						return false;
					if (m_EAccountType == k_EAccountTypeIndividual && (m_unAccountId == 0 || m_unAccountInstance != 1))
						return false;
					if (m_EAccountType == k_EAccountTypeClan && (m_unAccountId == 0 || m_unAccountInstance != 0))
						return false;
					return true;
				}
			}


			public override string ToString() {
				return $"{{account ID: {m_unAccountId}, account instance: {m_unAccountInstance}, " +
					   $"account type: {m_EAccountType}, universe: {m_EUniverse}}}";
			}


			public enum EUniverse {
				k_EUniverseInvalid = 0,
				k_EUniversePublic = 1,
				k_EUniverseBeta = 2,
				k_EUniverseInternal = 3,
				k_EUniverseDev = 4,
				k_EUniverseRC = 5,
				k_EUniverseMax
			}
			
			
			// Steam account types
			public enum EAccountType {
				k_EAccountTypeInvalid = 0,
				k_EAccountTypeIndividual = 1,        // single user account
				k_EAccountTypeMultiseat = 2,         // multiseat (e.g. cybercafe) account
				k_EAccountTypeGameServer = 3,        // game server account
				k_EAccountTypeAnonGameServer = 4,    // anonymous game server account
				k_EAccountTypePending = 5,           // pending
				k_EAccountTypeContentServer = 6,     // content server
				k_EAccountTypeClan = 7,
				k_EAccountTypeChat = 8,
				// k_EAccountTypeP2PSuperSeeder = 9, // unused
				k_EAccountTypeAnonUser = 10,

				// Max of 16 items in this field
				k_EAccountTypeMax
			}

			private const uint k_unSteamAccountIDMask = 0xFFFFFFFF;
			private const uint k_unSteamAccountInstanceMask = 0x000FFFFF;
			
			// Special flags for Chat accounts - they go in the top 8 bits
			// of the steam ID's "instance", leaving 12 for the actual instances
			public enum EChatSteamIDInstanceFlags : uint {
				k_EChatAccountInstanceMask = 0x00000FFF, // top 8 bits are flags

				k_EChatInstanceFlagClan = (k_unSteamAccountInstanceMask + 1) >> 1,     // top bit
				k_EChatInstanceFlagLobby = (k_unSteamAccountInstanceMask + 1) >> 2,    // next one down, etc
				k_EChatInstanceFlagMMSLobby = (k_unSteamAccountInstanceMask + 1) >> 3, // next one down, etc

				// Max of 8 flags
			}
		}
	}
}