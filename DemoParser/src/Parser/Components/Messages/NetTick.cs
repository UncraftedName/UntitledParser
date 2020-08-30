using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {
	
	public class NetTick : DemoMessage {
		
		private const float NetTickScaleUp = 100000.0f;

		public uint EngineTick;
		public float HostFrameTime;
		public float HostFrameTimeStdDev;
		
		
		public NetTick(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}


		internal override void ParseStream(BitStreamReader bsr) {
			EngineTick = bsr.ReadUInt();
			HostFrameTime = bsr.ReadUShort() / NetTickScaleUp;
			HostFrameTimeStdDev = bsr.ReadUShort() / NetTickScaleUp;
			SetLocalStreamEnd(bsr);
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void AppendToWriter(IndentedWriter iw) {
			iw.AppendLine($"engine tick: {EngineTick}");
			iw.AppendLine($"host frame time: {HostFrameTime}");
			iw.Append($"host frame time std dev: {HostFrameTimeStdDev}");
		}
	}
}