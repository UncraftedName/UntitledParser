using UncraftedDemoParser.Parser;
using UncraftedDemoParser.Parser.Components.Abstract;

namespace UncraftedDemoParser.Obsolete {
	
	public class OldNetTick : DemoComponent {

		public int Type; // NetMsgTypeBits long
		public int mnTick;
		public ushort HostFrameTime; // clamp( (int)( NET_TICK_SCALEUP * m_flHostFrameTime ), 0, 65535 )
		public ushort HostFrameTimeStdDev; // clamp( (int)( NET_TICK_SCALEUP * m_flHostFrameTimeStdDeviation ), 0, 65535 )
		// to get ^
		// (float)buffer.ReadUBitLong( 16 ) / NET_TICK_SCALEUP;
		
		
		public OldNetTick(byte[] data, SourceDemo demoRef) : base(data, demoRef) {}


		protected override void ParseBytes() {
			throw new System.NotImplementedException();
		}

		public override void UpdateBytes() {
			throw new System.NotImplementedException();
		}
	}
}