using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {
	
	public class SvcServerInfo : DemoMessage {

		public ushort NetworkProtocol;
		public uint ServerCount;
		public bool IsHltv;
		public bool IsDedicated;
		public uint ClientCrc;
		public ushort MaxClasses;
		public uint MapCrc;
		public byte PlayerCount;
		public byte MaxClients;
		private BitStreamReader _unknown;
		public BitStreamReader Unknown => _unknown.FromBeginning();
		public float TickInterval;
		public char Platform;
		public string GameDir;
		public string MapName;
		public string SkyName;
		public string HostName;
		
		
		public SvcServerInfo(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			NetworkProtocol = bsr.ReadUShort();
			ServerCount = bsr.ReadUInt();
			IsHltv = bsr.ReadBool();
			IsDedicated = bsr.ReadBool();
			ClientCrc = bsr.ReadUInt();
			MaxClasses = bsr.ReadUShort();
			MapCrc = bsr.ReadUInt(); // network protocol < 18
			PlayerCount = bsr.ReadByte();
			MaxClients = bsr.ReadByte();
			if (DemoRef.DemoSettings.Game == SourceDemoSettings.SourceGame.L4D2_2000) {
				_unknown = bsr.SubStream(33);
				bsr.SkipBits(33);
			} else if (DemoRef.DemoSettings.NewEngine) {
				_unknown = bsr.SubStream(32);
				bsr.SkipBytes(4);
			} else if (DemoRef.Header.NetworkProtocol == 24) {
				_unknown = bsr.SubStream(96);
				bsr.SkipBytes(12);
			}
			TickInterval = bsr.ReadFloat();
			Platform = (char)bsr.ReadByte();
			GameDir = bsr.ReadNullTerminatedString();
			MapName = bsr.ReadNullTerminatedString();
			SkyName = bsr.ReadNullTerminatedString();
			HostName = bsr.ReadNullTerminatedString();
			//HasReplay = bsr.ReadBool(); // protocol version ?
			SetLocalStreamEnd(bsr);

			DemoRef.DemoSettings.TickInterval = TickInterval;
			// this packet always(?) appears before the creation of any tables
			DemoRef.CStringTablesManager.Readable = true;
			DemoRef.CStringTablesManager.ClearCurrentTables();
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new System.NotImplementedException();
		}


		internal override void AppendToWriter(IndentedWriter iw) {
			iw.AppendLine($"network protocol: {NetworkProtocol}");
			iw.AppendLine($"server count: {ServerCount}");
			iw.AppendLine($"is hltv: {IsHltv}");
			iw.AppendLine($"is dedicated: {IsDedicated}");
			iw.AppendLine($"sever client CRC: {ClientCrc}"); // change to hex?
			iw.AppendLine($"max classes: {MaxClasses}");
			iw.AppendLine($"server map CRC: {MapCrc}"); // change to hex?
			iw.AppendLine($"current player count: {PlayerCount}");
			iw.AppendLine($"max player count: {MaxClients}");
			if (_unknown != null)
				iw.AppendLine($"unknown: 0x{Unknown.ToHexString().Replace(" ", " 0x")}");
			iw.AppendLine($"interval per tick: {TickInterval}");
			iw.AppendLine($"platform: {Platform}");
			iw.AppendLine($"game directory: {GameDir}");
			iw.AppendLine($"map name: {MapName}");
			iw.AppendLine($"skybox name: {SkyName}");
			iw.Append($"host name: {HostName}");
		}
	}
}