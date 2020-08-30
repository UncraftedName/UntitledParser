using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {
	
	public class NetFile : DemoMessage {

		public uint TransferId;
		public string FileName;
		public NetFileFlags FileFlags;
		
		
		public NetFile(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			TransferId = bsr.ReadUInt();
			FileName = bsr.ReadNullTerminatedString();
			FileFlags = (NetFileFlags)bsr.ReadBitsAsUInt(DemoSettings.NumNetFileFlagBits);
			SetLocalStreamEnd(bsr);
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void AppendToWriter(IndentedWriter iw) {
			iw.AppendLine($"transfer ID: {TransferId}");
			iw.AppendLine($"file name: {FileName}");
			iw.Append($"flags: {FileFlags}");
		}
	}


	[Flags]
	public enum NetFileFlags : uint {
		None          = 0,
		FileRequested = 1,
		Unknown       = 1 << 1
	}
}