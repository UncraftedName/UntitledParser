using System.Text;
using UncraftedDemoParser.Parser.Components.Abstract;
using UncraftedDemoParser.Utils;

namespace UncraftedDemoParser.Parser.Components.SvcNetMessages {
	
	public class NetSignOnState : SvcNetMessage {


		public byte SignOnState;
		public int SpawnCount;
		// the following are only set if the demo is new engine
		public int? NumServerPlayers;
		// public int NetworkIdsCount; // not stored for simplicity
		public byte[] PlayerNetworkIds; // todo - ensure this is in bytes
		// public int strLength; // not stored for simplicity
		public string MapName;
		
		
		public NetSignOnState(byte[] data, SourceDemo demoRef, int tick) : base(data, demoRef, tick) {}
		
		
		protected override void ParseBytes(BitFieldReader bfr) {
			SignOnState = bfr.ReadByte();
			SpawnCount = bfr.ReadInt();
			if (DemoRef.DemoSettings.NewEngine) {
				NumServerPlayers = bfr.ReadInt();
				int idCount = bfr.ReadInt();
				if (idCount > 0)
					PlayerNetworkIds = bfr.ReadBytes(idCount);
				int strLength = bfr.ReadInt();
				if (strLength > 0)
					MapName = bfr.ReadStringOfLength(strLength);
			}
		}


		protected override void PopulatedBuilder(StringBuilder builder) {
			builder.AppendLine($"\t\tsign on state: {SignOnState}");
			builder.AppendLine($"\t\tspawn count: {SpawnCount}");
			builder.AppendLine($"\t\tserver player count: {NumServerPlayers?.ToString() ?? "null"}");
			builder.AppendLine($"\t\tplayer network id length: {PlayerNetworkIds?.Length.ToString() ?? "null"}");
			builder.Append($"\t\tmap name: {MapName ?? "null"}");
		}
	}
}