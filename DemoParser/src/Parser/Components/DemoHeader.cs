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

		internal override void AppendToWriter(IndentedWriter iw) {
			iw += $"file stamp: {FileStamp}\n";
			iw += $"demo protocol: {DemoProtocol}\n";
			iw += $"network protocol: {NetworkProtocol}\n";
			iw += $"server name: {ServerName}\n";
			iw += $"client name: {ClientName}\n";
			iw += $"map name: {MapName}\n";
			iw += $"game directory: {GameDirectory}\n";
			iw += $"playback time: {PlaybackTime}\n";
			iw += $"tick count: {TickCount}\n";
			iw += $"frame count: {FrameCount}\n";
			iw += $"sign on length: {SignOnLength}\n";
		}
	}
}