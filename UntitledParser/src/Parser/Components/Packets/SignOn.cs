using System;
using UntitledParser.Parser.Components.Abstract;
using UntitledParser.Utils;

namespace UntitledParser.Parser.Components.Packets {
	
	public class SignOn : DemoPacket {

		public MessageStream MessageStream;
		
		
		public SignOn(SourceDemo demoRef, BitStreamReader reader, int tick) : base(demoRef, reader, tick) {}


		internal override void ParseStream(BitStreamReader bsr) {
			if (DemoRef.DemoSettings.Game == SourceDemoSettings.SourceGame.L4D2_2000) {
				bsr.SkipBytes(312);
			} else if (DemoRef.DemoSettings.NewEngine) {
				bsr.SkipBytes(160);
			} else {
				bsr.SkipBytes(84);
			}
			MessageStream = new MessageStream(DemoRef, bsr);
			MessageStream.ParseStream(bsr);
			SetLocalStreamEnd(bsr);
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		internal override void AppendToWriter(IndentedWriter iw) {
			/*var tmp = Reader; todo figure out if/how to properly include this
			iw.AppendLine((DemoRef.DemoSettings.NewEngine ? 152 : 76) + " bytes of null data (not 100% checked)");
			tmp.SkipBytes(DemoRef.DemoSettings.NewEngine ? 152 : 76);
			iw.AppendLine($"2 unknown ints: {tmp.ReadSInt()}, {tmp.ReadSInt()}");*/
			MessageStream.AppendToWriter(iw);
		}
	}
}