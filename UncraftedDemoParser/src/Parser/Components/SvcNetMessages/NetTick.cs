using System.Text;
using UncraftedDemoParser.Parser.Components.Abstract;
using UncraftedDemoParser.Utils;

namespace UncraftedDemoParser.Parser.Components.SvcNetMessages {
	
	public class NetTick : SvcNetMessage {
		

		private const float NetTickScaleUp = 100000.0f;

		public int EngineTick;
		public float HostFrameTime;
		public float HostFrameTimeStdDev;
		
		
		public NetTick(byte[] data, SourceDemo demoRef, int tick) : base(data, demoRef, tick) {}
		
		
		protected override void ParseBytes(BitFieldReader bfr) {
			EngineTick = bfr.ReadInt();
			HostFrameTime = bfr.ReadShort() / NetTickScaleUp;
			HostFrameTimeStdDev = bfr.ReadShort() / NetTickScaleUp;
		}


		protected override void PopulatedBuilder(StringBuilder builder) {
			builder.AppendLine($"\t\tengine tick: {EngineTick}");
			builder.AppendLine($"\t\thost frame time: {HostFrameTime}");
			builder.Append($"\t\thost frame time std dev: {HostFrameTimeStdDev}");
		}
	}
}