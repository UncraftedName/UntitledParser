using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages.UserMessages {
	
	public class MpMapCompleted : UserMessage {

		public byte Branch; // course probably
		public byte Level;
		
		
		public MpMapCompleted(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			Branch = bsr.ReadByte();
			Level = bsr.ReadByte();
		}
		
		
		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}
		
		
		public override void AppendToWriter(IndentedWriter iw) {
			iw.AppendLine($"branch: {Branch}");
			iw.Append($"level: {Level}");
		}
	}
	
	
	public class MpMapIncomplete : MpMapCompleted {
		public MpMapIncomplete(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
	} 
}
