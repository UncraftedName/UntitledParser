using System.Numerics;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages.UserMessages {
	
	public class EntityPortalled : SvcUserMessage {

		public int PortalEntIndex;
		public int PortalSerialNum;
		public int PortalledEntIndex;
		public int PortalledEntSerialNum;
		public Vector3 NewPosition;
		public Vector3 NewAngles;
		
		
		public EntityPortalled(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			uint eHandle = bsr.ReadUInt();
			PortalEntIndex = (int)(eHandle & ((1 << DemoSettings.MaxEdictBits) - 1));
			PortalSerialNum = (int)(eHandle >> DemoSettings.MaxEdictBits);
			eHandle = bsr.ReadUInt();
			PortalledEntIndex = (int)(eHandle & ((1 << DemoSettings.MaxEdictBits) - 1));
			PortalledEntSerialNum = (int)(eHandle >> DemoSettings.MaxEdictBits);
			NewPosition = bsr.ReadVector3();
			NewAngles = bsr.ReadVector3();
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new System.NotImplementedException();
		}


		public override void AppendToWriter(IndentedWriter iw) {
			iw.AppendLine($"portal entity index: {PortalEntIndex}");
			iw.AppendLine($"portal serial num: {PortalSerialNum}");
			iw.AppendLine($"portalled entity index: {PortalledEntIndex}");
			iw.AppendLine($"portalled entity serial num: {PortalledEntSerialNum}");
			iw.AppendLine($"new position: {NewPosition:F3}");
			iw.Append($"new angles: <{NewAngles.X:F3}°, {NewAngles.Y:F3}°, {NewAngles.Z:F3}°>");
		}
	}
}