using System.Text;
using UncraftedDemoParser.Parser.Components.Abstract;
using UncraftedDemoParser.Utils;

namespace UncraftedDemoParser.Parser.Components.SvcNetMessages {
	
	
	public class SvcSetPause : SvcNetMessage {


		public bool IsPaused;
		
		
		public SvcSetPause(byte[] data, SourceDemo demoRef, int tick) : base(data, demoRef, tick) {}
		
		
		protected override void ParseBytes(BitFieldReader bfr) {
			IsPaused = bfr.ReadBool();
		}


		protected override void PopulatedBuilder(StringBuilder builder) {
			builder.Append($"\t\tis paused: {IsPaused}");
		}
	}
}