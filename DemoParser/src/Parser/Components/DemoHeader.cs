using System;
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
		
		
		public DemoHeader(SourceDemo? demoRef) : base(demoRef) {}

		protected override void Parse(ref BitStreamReader bsr) {
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
		}

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
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