using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {
	
	public class NetFile : DemoMessage {

		public uint TransferID;
		public string FileName;
		public NetFileFlags FileFlags; // i'm just guessing that these are flags, but there's two bits
		
		
		public NetFile(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			TransferID = bsr.ReadUInt();
			FileName = bsr.ReadNullTerminatedString();
			FileFlags = (NetFileFlags)bsr.ReadBitsAsUInt(2); // might be portal 2 only
			SetLocalStreamEnd(bsr);
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void AppendToWriter(IndentedWriter iw) {
			iw.AppendLine($"transfer ID: {TransferID}");
			iw.AppendLine($"file name: {FileName}");
			iw.Append($"flags: {FileFlags}");
		}
	}


	[Flags]
	public enum NetFileFlags : uint {
		None			= 0,
		FileRequested 	= 1,
		Unknown 		= 1 << 1
	}
}