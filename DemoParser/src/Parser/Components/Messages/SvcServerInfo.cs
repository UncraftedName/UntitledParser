using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.HelperClasses.EntityStuff;
using DemoParser.Parser.HelperClasses.GameState;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {

	public class SvcServerInfo : DemoMessage {

		public ushort NetworkProtocol;
		public uint ServerCount;
		public bool IsHltv;
		public bool? UnknownBit;
		public bool IsDedicated;
		public int ClientCrc;
		public ushort MaxServerClasses;
		public byte[]? MapMD5;
		public uint? MapCrc;
		public byte PlayerCount;
		public byte MaxClients;
		public uint? Unknown;
		public float TickInterval;
		public char Platform;
		public string GameDir;
		public string MapName;
		public string SkyName;
		public string HostName;
		public string? MissionName;
		public string? MutationName;
		public bool? HasReplay;

		public int GameDirBitIndex; // used for game directory changing

		public SvcServerInfo(SourceDemo? demoRef) : base(demoRef) {}


		// src_main/common/netmessages.cpp  SVC_ServerInfo::WriteToBuffer
		protected override void Parse(ref BitStreamReader bsr) {
			NetworkProtocol = bsr.ReadUShort();
			ServerCount = bsr.ReadUInt();
			IsHltv = bsr.ReadBool();
			IsDedicated = bsr.ReadBool();
			if (DemoInfo.IsLeft4Dead() && DemoInfo.Game >= SourceGame.L4D2_2220)
				UnknownBit = bsr.ReadBool();
			ClientCrc = bsr.ReadSInt();
			if (DemoInfo.NewDemoProtocol) // unknown field, could be before ClientCrc
				Unknown = bsr.ReadUInt();
			MaxServerClasses = bsr.ReadUShort();
			if (NetworkProtocol == 24)
				MapMD5 = bsr.ReadBytes(16);
			else
				MapCrc = bsr.ReadUInt(); // network protocol < 18 according to p2 leak, but doesn't add up for l4d2 and p2
			PlayerCount = bsr.ReadByte();
			MaxClients = bsr.ReadByte();
			TickInterval = bsr.ReadFloat();
			Platform = (char)bsr.ReadByte();
			GameDirBitIndex = bsr.AbsoluteBitIndex;
			GameDir = bsr.ReadNullTerminatedString();
			MapName = bsr.ReadNullTerminatedString();
			SkyName = bsr.ReadNullTerminatedString();
			HostName = bsr.ReadNullTerminatedString();
			if (DemoInfo.IsLeft4Dead() && DemoInfo.Game >= SourceGame.L4D2_2220)
			{
				MissionName = bsr.ReadNullTerminatedString();
				MutationName = bsr.ReadNullTerminatedString();
			}
			if (NetworkProtocol == 24)
				HasReplay = bsr.ReadBool(); // protocol version >= 16

			// l4d read multiple SvcServerInfo messages and broke the TickInterval parsed
			// in the first message (that is parsed correctly)
			// prevent the interval from being overwritten
			if (!DemoInfo.HasParsedTickInterval)
			{
				DemoInfo.TickInterval = TickInterval;
				DemoInfo.HasParsedTickInterval = true;
			}
			// this packet always(?) appears before the creation of any tables

			DemoRef.StringTablesManager.ClearCurrentTables();

			// init baselines here
			DemoRef.EntBaseLines = new EntityBaseLines(DemoRef, MaxServerClasses);
		}


		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.AppendLine($"network protocol: {NetworkProtocol}");
			pw.AppendLine($"server count: {ServerCount}");
			pw.AppendLine($"is hltv: {IsHltv}");
			pw.AppendLine($"is dedicated: {IsDedicated}");
			if (UnknownBit != null)
				pw.AppendLine($"unknown bit: {UnknownBit}");
			pw.AppendLine($"server client CRC: {ClientCrc}"); // change to hex?
			if (Unknown != null)
				pw.AppendLine($"unknown byte: {Unknown}");
			pw.AppendLine($"max server classes: {MaxServerClasses}");
			pw.AppendLine(MapMD5 != null
				? $"server map MD5: 0x{BitConverter.ToString(MapMD5).Replace("-", "")}"
				: $"server map CRC: {MapCrc}");
			pw.AppendLine($"current player count: {PlayerCount}");
			pw.AppendLine($"max player count: {MaxClients}");
			pw.AppendLine($"interval per tick: {TickInterval}");
			pw.AppendLine($"platform: {Platform}");
			pw.AppendLine($"game directory: {GameDir}");
			pw.AppendLine($"map name: {MapName}");
			pw.AppendLine($"skybox name: {SkyName}");
			pw.Append($"host name: {HostName}");
			if (MissionName != null && MutationName != null)
			{
				pw.AppendLine($"\nmission name: {MissionName}");
				pw.Append($"mutation name: {MutationName}");
			}
			if (HasReplay != null)
				pw.Append($"\nhas replay: {HasReplay}");
		}
	}
}
