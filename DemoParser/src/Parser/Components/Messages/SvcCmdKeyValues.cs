using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {
	
	
	public class SvcCmdKeyValues : DemoMessage {

		public byte[] Arr; // todo
		
		
		public SvcCmdKeyValues(SourceDemo? demoRef) : base(demoRef) {}


		protected override void Parse(ref BitStreamReader bsr) {
			Arr = bsr.ReadBytes((int)bsr.ReadUInt());
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.Append($"array of length: {Arr.Length}");
		}
	}
}