using System.Text;
using UncraftedDemoParser.Parser.Components.Abstract;
using UncraftedDemoParser.Utils;

namespace UncraftedDemoParser.Parser.Components.SvcNetMessages {
	
	
	public class SvcSetPause : SvcNetMessage {


		public bool IsPaused;
		
		
		public SvcSetPause(byte[] data, SourceDemo demoRef, int tick) : base(data, demoRef, tick) {}
		
		
		protected override void ParseBytes(BitFieldReader bfr) {
			IsPaused = bfr.Length != 0 && bfr.ReadBool(); // idk how to handle this better, sometimes the data length is 0
		}


		protected override void PopulatedBuilder(StringBuilder builder) {
			builder.Append($"\t\tis paused: {IsPaused}");
		}
	}
}