using System.Numerics;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages.UserMessages {
	
	public class Damage : SvcUserMessage {

		public byte Armor;
		public byte DamageTaken;
		public int BitsDamage;
		public Vector3 VecFrom;
		
		
		public Damage(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			Armor = bsr.ReadByte();
			DamageTaken = bsr.ReadByte();
			BitsDamage = bsr.ReadSInt();
			VecFrom = bsr.ReadVector3();
			/*VecFrom = new Vector3 { 		old engine?
				X = bsr.ReadCoord(),
				Y = bsr.ReadCoord(),
				Z = bsr.ReadCoord()
			};*/
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new System.NotImplementedException();
		}


		public override void AppendToWriter(IndentedWriter iw) {
			iw.AppendLine($"armor: {Armor}");
			iw.AppendLine($"damage taken: {DamageTaken}");
			iw.AppendLine($"bits damage: {BitsDamage}");
			iw.Append($"vec from: {VecFrom:F3}");
		}
	}
}