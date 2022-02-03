using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {

	public class NetFile : DemoMessage {

		public uint TransferId;
		public string FileName;
		public NetFileFlags FileFlags;


		public NetFile(SourceDemo? demoRef) : base(demoRef) {}


		protected override void Parse(ref BitStreamReader bsr) {
			TransferId = bsr.ReadUInt();
			FileName = bsr.ReadNullTerminatedString();
			FileFlags = (NetFileFlags)bsr.ReadUInt(DemoInfo.NumNetFileFlagBits);
		}


		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.AppendLine($"transfer ID: {TransferId}");
			pw.AppendLine($"file name: {FileName}");
			pw.Append($"flags: {FileFlags}");
		}
	}


	[Flags]
	public enum NetFileFlags : uint {
		None          = 0,
		FileRequested = 1,
		Unknown       = 1 << 1
	}
}
