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
		public uint ClientCrc;
		public ushort MaxServerClasses;
		public uint MapCrc;
		public byte PlayerCount;
		public byte MaxClients;
		private BitStreamReader? _unknown;
		public BitStreamReader? Unknown => _unknown?.FromBeginning();
		public float TickInterval;
		public char Platform;
		public string GameDir;
		public string MapName;
		public string SkyName;
		public string HostName;
		
		
		public SvcServerInfo(SourceDemo? demoRef) : base(demoRef) {}
		
		
		// src_main/common/netmessages.cpp  SVC_ServerInfo::WriteToBuffer
		protected override void Parse(ref BitStreamReader bsr) {
			NetworkProtocol = bsr.ReadUShort();
			ServerCount = bsr.ReadUInt();
			IsHltv = bsr.ReadBool();
			IsDedicated = bsr.ReadBool();
			ClientCrc = bsr.ReadUInt();
			MaxServerClasses = bsr.ReadUShort();
			MapCrc = bsr.ReadUInt(); // network protocol < 18?
			PlayerCount = bsr.ReadByte();
			MaxClients = bsr.ReadByte();
			int skip = DemoInfo.SvcServerInfoUnknownBits; // todo, also fields are out of order for p2
			if (skip != 0)
				_unknown = bsr.SplitAndSkip(skip);
			TickInterval = bsr.ReadFloat();
			Platform = (char)bsr.ReadByte();
			GameDir = bsr.ReadNullTerminatedString();
			MapName = bsr.ReadNullTerminatedString();
			SkyName = bsr.ReadNullTerminatedString();
			HostName = bsr.ReadNullTerminatedString();
			//HasReplay = bsr.ReadBool(); // protocol version ?

			DemoInfo.TickInterval = TickInterval;
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
			iw.AppendLine($"max server classes: {MaxServerClasses}");
			iw.AppendLine($"server map CRC: {MapCrc}"); // change to hex?
			iw.AppendLine($"current player count: {PlayerCount}");
			iw.AppendLine($"max player count: {MaxClients}");
			var tmp = Unknown;
			if (tmp.HasValue)
				iw.AppendLine($"{tmp.Value.BitLength} unknown bits: 0x{tmp.Value.ToHexString().Replace(" ", " 0x")}");
			iw.AppendLine($"interval per tick: {TickInterval}");
			iw.AppendLine($"platform: {Platform}");
			iw.AppendLine($"game directory: {GameDir}");
			iw.AppendLine($"map name: {MapName}");
			iw.AppendLine($"skybox name: {SkyName}");
			iw.Append($"host name: {HostName}");
		}
	}
}