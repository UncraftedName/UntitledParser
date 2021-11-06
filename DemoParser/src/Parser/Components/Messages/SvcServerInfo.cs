using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.HelperClasses.EntityStuff;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {
	
	public class SvcServerInfo : DemoMessage {

		public ushort NetworkProtocol;
		public uint ServerCount;
		public bool IsHltv;
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
		public bool? HasReplay;

		public int GameDirBitIndex; // used for game directory changing
		
		public SvcServerInfo(SourceDemo? demoRef) : base(demoRef) {}
		
		
		// src_main/common/netmessages.cpp  SVC_ServerInfo::WriteToBuffer
		protected override void Parse(ref BitStreamReader bsr) {
			NetworkProtocol = bsr.ReadUShort();
			ServerCount = bsr.ReadUInt();
			IsHltv = bsr.ReadBool();
			IsDedicated = bsr.ReadBool();
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
			if (NetworkProtocol == 24)
				HasReplay = bsr.ReadBool(); // protocol version >= 16

			// l4d read multiple SvcServerInfo messages and broke the TickInterval parsed
			// in the first message (that is parsed correctly)
			// prevent the interval from being overwritten
			if (DemoInfo.HasParsedTickInterval)
			{
				DemoInfo.TickInterval = TickInterval;
				DemoInfo.HasParsedTickInterval = true;
			}
			// this packet always(?) appears before the creation of any tables
			
			DemoRef.CurStringTablesManager.ClearCurrentTables();
			
			// init baselines here
			DemoRef.CBaseLines = new CurBaseLines(DemoRef, MaxServerClasses);
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void PrettyWrite(IPrettyWriter iw) {
			iw.AppendLine($"network protocol: {NetworkProtocol}");
			iw.AppendLine($"server count: {ServerCount}");
			iw.AppendLine($"is hltv: {IsHltv}");
			iw.AppendLine($"is dedicated: {IsDedicated}");
			iw.AppendLine($"server client CRC: {ClientCrc}"); // change to hex?
			if (Unknown != null)
				iw.AppendLine($"unknown byte: {Unknown}");
			iw.AppendLine($"max server classes: {MaxServerClasses}");
			if (MapCrc == null)
				iw.AppendLine($"server map MD5: 0x{BitConverter.ToString(MapMD5).Replace("-","")}");
			else
				iw.AppendLine($"server map CRC: {MapCrc}"); // change to hex?
			iw.AppendLine($"current player count: {PlayerCount}");
			iw.AppendLine($"max player count: {MaxClients}");
			iw.AppendLine($"interval per tick: {TickInterval}");
			iw.AppendLine($"platform: {Platform}");
			iw.AppendLine($"game directory: {GameDir}");
			iw.AppendLine($"map name: {MapName}");
			iw.AppendLine($"skybox name: {SkyName}");
			iw.Append($"host name: {HostName}");
			if (HasReplay != null)
				iw.Append($"\nhas replay: {HasReplay}");
		}
	}
}