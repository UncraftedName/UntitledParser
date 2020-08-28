using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Packets.CustomDataTypes {
	
	public class MenuCallBack : CustomDataMessage {

		public int Unknown;
		public string CallbackStr;
		
		
		public MenuCallBack(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			Unknown = bsr.ReadSInt();
			CallbackStr = bsr.ReadNullTerminatedString();
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void AppendToWriter(IndentedWriter iw) {
			iw.AppendLine($"unknown: {Unknown}");
			iw.Append($"callback string: {CallbackStr}");
		}
	}
}