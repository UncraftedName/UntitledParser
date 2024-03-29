using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components {

	public class DemoHeader : DemoComponent {

		public string FileStamp;
		public uint DemoProtocol;
		public uint NetworkProtocol;
		public string ServerName;
		public string ClientName;
		public string MapName;
		public string GameDirectory;
		public float PlaybackTime;
		public int TickCount;
		public int FrameCount;
		public uint SignOnLength;


		public DemoHeader(SourceDemo? demoRef) : base(demoRef) {}

		protected override void Parse(ref BitStreamReader bsr) {
			FileStamp = bsr.ReadStringOfLength(8);
			DemoProtocol = bsr.ReadUInt();
			NetworkProtocol = bsr.ReadUInt();
			ServerName = bsr.ReadStringOfLength(260);
			ClientName = bsr.ReadStringOfLength(260);
			MapName = bsr.ReadStringOfLength(260);
			GameDirectory = bsr.ReadStringOfLength(260);
			PlaybackTime = bsr.ReadFloat();
			TickCount = bsr.ReadSInt();
			FrameCount = bsr.ReadSInt();
			SignOnLength = bsr.ReadUInt();
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.AppendLine($"file stamp: {FileStamp}");
			pw.AppendLine($"demo protocol: {DemoProtocol}");
			pw.AppendLine($"network protocol: {NetworkProtocol}");
			pw.AppendLine($"server name: {ServerName}");
			pw.AppendLine($"client name: {ClientName}");
			pw.AppendLine($"map name: {MapName}");
			pw.AppendLine($"game directory: {GameDirectory}");
			pw.AppendLine($"playback time: {PlaybackTime}");
			pw.AppendLine($"tick count: {TickCount}");
			pw.AppendLine($"frame count: {FrameCount}");
			pw.Append($"sign on length: {SignOnLength}");
		}
	}
}
