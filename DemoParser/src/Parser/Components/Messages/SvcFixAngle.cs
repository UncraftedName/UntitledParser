using System;
using System.Numerics;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {

	public class SvcFixAngle : DemoMessage {

		public bool Relative;
		public Vector3 Angle;

		public SvcFixAngle(SourceDemo? demoRef) : base(demoRef) {}


		protected override void Parse(ref BitStreamReader bsr) {
			Relative = bsr.ReadBool();
			Angle = new Vector3 {
				X = bsr.ReadBitAngle(16),
				Y = bsr.ReadBitAngle(16),
				Z = bsr.ReadBitAngle(16),
			};
		}


		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.AppendLine($"relative: {Relative}");
			pw.Append($"Angle: <{Angle.X:F4}°, {Angle.Y:F4}°, {Angle.Z:F4}°>");
		}
	}
}
