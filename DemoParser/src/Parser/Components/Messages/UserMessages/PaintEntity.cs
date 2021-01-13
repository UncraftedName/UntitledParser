using System.Numerics;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages.UserMessages {
	
	public class PaintEntity : UserMessage {

		public EHandle Ent;
		public PaintType PaintType;
		public Vector3 Pos;
		
		
		public PaintEntity(SourceDemo? demoRef) : base(demoRef) {}
		
		
		protected override void Parse(ref BitStreamReader bsr) {
			Ent = bsr.ReadEHandle();
			PaintType = (PaintType)bsr.ReadByte();
			bsr.ReadVector3(out Pos);
		}


		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new System.NotImplementedException();
		}


		public override void PrettyWrite(IPrettyWriter iw) {
			iw.AppendLine($"entity: {Ent}");
			iw.AppendLine($"paint type: {PaintType.ToString()}");
			iw.Append($"pos: {Pos}");
		}
	}
}