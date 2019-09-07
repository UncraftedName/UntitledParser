using UncraftedDemoParser.DemoStructure.Components.Abstract;
using UncraftedDemoParser.Utils;

namespace UncraftedDemoParser.DemoStructure.Components {
	
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
	}
}