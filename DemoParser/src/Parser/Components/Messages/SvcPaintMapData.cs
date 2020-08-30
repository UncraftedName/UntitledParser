using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {
	
	public class SvcPaintMapData : DemoMessage {

		// maybe try CPaintmapDataManager::GetPaintmapDataRLE
		private BitStreamReader _data; // todo, it looks like this uses RLE so guess i'll die
		public BitStreamReader Data => _data.FromBeginning();
		
		
		public SvcPaintMapData(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			_data = bsr.SubStream(bsr.ReadUInt());
			SetLocalStreamEnd(bsr);
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void AppendToWriter(IndentedWriter iw) {
			iw.Append($"data length in bits: {_data.BitLength}");
		}
	}
}