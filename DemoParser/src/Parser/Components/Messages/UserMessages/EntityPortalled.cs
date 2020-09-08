using System;
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
		
		
		public EntityPortalled(SourceDemo? demoRef) : base(demoRef) {}


		protected override void Parse(ref BitStreamReader bsr) {
			Portal = bsr.ReadEHandle();
			Portalled = bsr.ReadEHandle();
			bsr.ReadVector3(out NewPosition);
			bsr.ReadVector3(out NewAngles);
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void AppendToWriter(IIndentedWriter iw) {
			iw.AppendLine($"portal: {Portal}");
			iw.AppendLine($"portalled: {Portalled}");
			iw.AppendLine($"new position: {NewPosition:F3}");
			iw.Append($"new angles: <{NewAngles.X:F3}°, {NewAngles.Y:F3}°, {NewAngles.Z:F3}°>");
		}
	}
}