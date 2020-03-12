using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages.UserMessages {
	
	public class KeyHintText : SvcUserMessage {

		public int Count; // should always be 1 lmao
		public string KeyString;
		
		
		public KeyHintText(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			Count = bsr.ReadByte();
			if (Count != 1)
				DemoRef.AddError($"{GetType()} borking, there should only be one string but count is {Count}");
			KeyString = bsr.ReadNullTerminatedString();
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		internal override void AppendToWriter(IndentedWriter iw) {
			iw.AppendLine($"count: {Count}");
			iw.Append($"str: {KeyString}");
		}
	}
}