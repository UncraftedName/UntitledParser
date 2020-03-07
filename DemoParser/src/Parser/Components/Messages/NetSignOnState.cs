using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;

namespace DemoParser.Parser.Components.Messages {
	
	public class NetSignOnState : DemoMessage {
		
		public SignOnState SignOnState;
		public int SpawnCount;
		// new engine only
		public uint? NumServerPlayers;
		public byte[] PlayerNetworkIds;
		public string MapName;
		
		
		public NetSignOnState(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			SignOnState = (SignOnState)bsr.ReadByte();
			SpawnCount = bsr.ReadSInt();
			if (DemoRef.DemoSettings.NewEngine) {
				NumServerPlayers = bsr.ReadUInt();
				int length = (int)bsr.ReadUInt();
				if (length > 0)
					PlayerNetworkIds = bsr.ReadBytes(length);
				length = (int)bsr.ReadUInt();
				if (length > 0) // the string still seams to be null terminated (sometimes?)
					MapName = bsr.ReadStringOfLength(length).Split('\0', 2)[0];
			}
			SetLocalStreamEnd(bsr);
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new System.NotImplementedException();
		}


		internal override void AppendToWriter(IndentedWriter iw) {
			iw.AppendLine($"sign on state: {SignOnState}");
			iw.Append($"spawn count: {SpawnCount}");
			if (DemoRef.DemoSettings.NewEngine) {
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