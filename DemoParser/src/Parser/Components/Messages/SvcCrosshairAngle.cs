using System.Numerics;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {
	
	public class SvcCrosshairAngle : DemoMessage {

		public Vector3 Angle;
		
		
		public SvcCrosshairAngle(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			Angle = new Vector3 {
				X = bsr.ReadBitAngle(16),
				Y = bsr.ReadBitAngle(16),
				Z = bsr.ReadBitAngle(16),
			};
			SetLocalStreamEnd(bsr);
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new System.NotImplementedException();
		}


		public override void AppendToWriter(IndentedWriter iw) {
			iw.Append($"Angle: <{Angle.X:F4}°, {Angle.Y:F4}°, {Angle.Z:F4}°>");
		}
	}
}