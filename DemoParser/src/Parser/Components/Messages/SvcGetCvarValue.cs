using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {
	
	public class SvcGetCvarValue : DemoMessage {

		public int Cookie;
		public string CvarName;
		
		
		public SvcGetCvarValue(SourceDemo? demoRef) : base(demoRef) {}


		protected override void Parse(ref BitStreamReader bsr) {
			Cookie = bsr.ReadSInt();
			CvarName = bsr.ReadNullTerminatedString();
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void AppendToWriter(IIndentedWriter iw) {
			iw.AppendLine($"cookie: {Cookie}");
			iw.Append($"cvar name: {CvarName}");
		}
	}
}