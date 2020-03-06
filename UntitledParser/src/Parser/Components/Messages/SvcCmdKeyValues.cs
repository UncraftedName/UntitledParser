using System;
using UntitledParser.Parser.Components.Abstract;
using UntitledParser.Utils;

namespace UntitledParser.Parser.Components.Messages {
	
	
	public class SvcCmdKeyValues : DemoMessage {

		public byte[] Arr; // todo
		
		
		public SvcCmdKeyValues(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			Arr = bsr.ReadBytes((int)bsr.ReadUInt());
			SetLocalStreamEnd(bsr);
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		internal override void AppendToWriter(IndentedWriter iw) {
			iw += $"array of length: {Arr.Length}";
		}
	}
}