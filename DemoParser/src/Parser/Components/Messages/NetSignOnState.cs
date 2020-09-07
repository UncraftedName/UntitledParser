using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {
	
	public class NetSignOnState : DemoMessage {
		
		public SignOnState SignOnState;
		public int SpawnCount;
		// demo protocol 4 only
		public uint? NumServerPlayers;
		public byte[]? PlayerNetworkIds;
		public string? MapName;
		
		
		public NetSignOnState(SourceDemo? demoRef) : base(demoRef) {}


		protected override void Parse(ref BitStreamReader bsr) {
			SignOnState = (SignOnState)bsr.ReadByte();
			SpawnCount = bsr.ReadSInt();
			if (DemoSettings.NewDemoProtocol) {
				NumServerPlayers = bsr.ReadUInt();
				int length = (int)bsr.ReadUInt();
				if (length > 0)
					PlayerNetworkIds = bsr.ReadBytes(length);
				length = (int)bsr.ReadUInt();
				if (length > 0) // the string still seams to be null terminated (sometimes?)
					MapName = bsr.ReadStringOfLength(length).Split(new char[]{'\0'}, 2)[0];
			}
			if (SignOnState == SignOnState.PreSpawn)
				DemoRef.ClientSoundSequence = 1; // reset sound sequence number after receiving SignOn sounds
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void AppendToWriter(IIndentedWriter iw) {
			iw.AppendLine($"sign on state: {SignOnState}");
			iw.Append($"spawn count: {SpawnCount}");
			if (DemoSettings.NewDemoProtocol) {
				iw.Append($"\nnumber of server players: {NumServerPlayers}");
				if (PlayerNetworkIds != null)
					iw.Append($"\nbyte array of length {PlayerNetworkIds.Length}");
				if (MapName != null)
					iw.Append($"\nmap name: {MapName}");
			}
		}
	}
	
	public enum SignOnState {
		None = 0,   // no state yet, about to connect
		Challenge,  // client challenging server, all OOB packets
		Connected,  // client is connected to server, netchans ready
		New,        // just got server info and string tables
		PreSpawn,   // received signon buggers
		Spawn,      // ready to receive entity packets
		Full,       // we are fully connected, first non-delta packet received
		ChangeLevel // server is changing level, please wait
	}
}