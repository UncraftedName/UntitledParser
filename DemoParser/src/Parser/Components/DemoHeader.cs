using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components {
	
	public class DemoHeader : DemoComponent {

		public CharArray FileStamp;
		public uint DemoProtocol;
		public uint NetworkProtocol;
		public CharArray ServerName;
		public CharArray ClientName;
		public CharArray MapName;
		public CharArray GameDirectory;
		public float PlaybackTime;
		public int TickCount;
		public int FrameCount;
		public uint SignOnLength;
		
		
		public DemoHeader(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}

		internal override void ParseStream(BitStreamReader bsr) {
			FileStamp = bsr.ReadCharArray(8);
			DemoProtocol = bsr.ReadUInt();
			NetworkProtocol = bsr.ReadUInt();
			ServerName = bsr.ReadCharArray(260);
			ClientName = bsr.ReadCharArray(260);
			MapName = bsr.ReadCharArray(260);
			GameDirectory = bsr.ReadCharArray(260);
			PlaybackTime = bsr.ReadFloat();
			TickCount = bsr.ReadSInt();
			FrameCount = bsr.ReadSInt();
			SignOnLength = bsr.ReadUInt();
			SetLocalStreamEnd(bsr);
		}

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new System.NotImplementedException();
		}


		public override void AppendToWriter(IndentedWriter iw) {
			iw.AppendLine($"file stamp: {FileStamp}");
			iw.AppendLine($"demo protocol: {DemoProtocol}");
			iw.AppendLine($"network protocol: {NetworkProtocol}");
			iw.AppendLine($"server name: {ServerName}");
			iw.AppendLine($"client name: {ClientName}");
			iw.AppendLine($"map name: {MapName}");
			iw.AppendLine($"game directory: {GameDirectory}");
			iw.AppendLine($"playback time: {PlaybackTime}");
			iw.AppendLine($"tick count: {TickCount}");
			iw.AppendLine($"frame count: {FrameCount}");
			iw.Append($"sign on length: {SignOnLength}");
		}
	}
}