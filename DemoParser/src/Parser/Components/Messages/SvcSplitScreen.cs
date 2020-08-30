using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {
	
	public class SvcSplitScreen : DemoMessage {

		public bool RemoveUser;
		public BitStreamReader Data {get;private set;}


		public SvcSplitScreen(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}


		internal override void ParseStream(BitStreamReader bsr) {
			RemoveUser = bsr.ReadBool();
			uint dataLen = bsr.ReadBitsAsUInt(11);
			Data = bsr.SubStream(dataLen);
			bsr.SkipBits(dataLen);
			SetLocalStreamEnd(bsr);
		}


		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void AppendToWriter(IndentedWriter iw) {
			iw.AppendLine(RemoveUser ? "remove user" : "add user");
			iw.Append($"data of length {Data.BitLength}");
		}
	}
}