using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {
	
	public class SvcSetView : DemoMessage {

		public uint EntityIndex;
		
		
		public SvcSetView(SourceDemo? demoRef) : base(demoRef) {}


		protected override void Parse(ref BitStreamReader bsr) {
			EntityIndex = bsr.ReadBitsAsUInt(DemoSettings.MaxEdictBits);
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void PrettyWrite(IPrettyWriter iw) {
			iw.Append($"entity index: {EntityIndex}");
		}
	}
}