using UntitledParser.Parser.Components.Abstract;
using UntitledParser.Utils;

namespace UntitledParser.Parser.Components.Packets.CustomDataTypes {
	
	public class Ping : CustomDataMessage {

		public uint X, Y;
		
		public Ping(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			X = bsr.ReadUInt();
			Y = bsr.ReadUInt();
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new System.NotImplementedException();
		}


		internal override void AppendToWriter(IndentedWriter iw) {
			iw.Append($"ping screen location (x, y): ({X}, {Y})");
		}
	}
}