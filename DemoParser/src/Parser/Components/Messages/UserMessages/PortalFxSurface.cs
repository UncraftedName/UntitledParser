using System;
using System.Numerics;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages.UserMessages {
	
	public class PortalFxSurface : UserMessage {
		
		public ushort PortalEnt;
		public ushort OwnerEnt;
		public byte Team;
		public byte PortalNum;
		public PortalFizzleType Effect;
		public Vector3 Origin;
		public Vector3 Angles;
		
		
		public PortalFxSurface(SourceDemo? demoRef) : base(demoRef) {}


		protected override void Parse(ref BitStreamReader bsr) {
			PortalEnt = bsr.ReadUShort();
			OwnerEnt = bsr.ReadUShort();
			Team = bsr.ReadByte();
			PortalNum = bsr.ReadByte();
			Effect = (PortalFizzleType)bsr.ReadByte();
			bsr.ReadVectorCoord(out Origin);
			bsr.ReadVectorCoord(out Angles);
		}
		
		
		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}
		
		
		public override void PrettyWrite(IPrettyWriter iw) {
			iw.AppendLine($"portal entity: {PortalEnt}");
			iw.AppendLine($"owner entity: {OwnerEnt}");
			iw.AppendLine($"team: {Team}");
			iw.AppendLine($"portal num: {PortalNum}");
			iw.AppendLine($"effect: {Effect}");
			iw.AppendLine($"origin: {Origin}");
			iw.Append($"angles: {Angles}");
		}
	}


	public enum PortalFizzleType : byte {
		PortalFizzleSuccess = 0, // Placed fine (no fizzle)
		PortalFizzleCantFit,
		PortalFizzleOverlappedLinked,
		PortalFizzleBadVolume,
		PortalFizzleBadSurface,
		PortalFizzleKilled,
		PortalFizzleCleanser,
		PortalFizzleClose,
		PortalFizzleNearBlue,
		PortalFizzleNearRed,
		PortalFizzleNone,
	}
}
