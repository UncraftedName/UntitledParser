using System.Numerics;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages.UserMessages {

	public class EntityPortalled : UserMessage {

		public EHandle Portal;
		public EHandle Portalled;
		public Vector3 NewPosition;
		public Vector3 NewAngles;


		public EntityPortalled(SourceDemo? demoRef, byte value) : base(demoRef, value) {}


		protected override void Parse(ref BitStreamReader bsr) {
			Portal = bsr.ReadEHandle();
			Portalled = bsr.ReadEHandle();
			bsr.ReadVector3(out NewPosition);
			bsr.ReadVector3(out NewAngles);
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.AppendLine($"portal: {Portal}");
			pw.AppendLine($"portalled: {Portalled}");
			pw.AppendLine($"new position: {NewPosition:F3}");
			pw.Append($"new angles: <{NewAngles.X:F3}°, {NewAngles.Y:F3}°, {NewAngles.Z:F3}°>");
		}
	}
}
