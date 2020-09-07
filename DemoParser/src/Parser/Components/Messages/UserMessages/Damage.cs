using System;
using System.Numerics;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages.UserMessages {
	
	public class Damage : UserMessage {

		public byte Armor;
		public byte DamageTaken;
		public int BitsDamage;
		public Vector3 VecFrom;
		
		
		public Damage(SourceDemo? demoRef) : base(demoRef) {}


		protected override void Parse(ref BitStreamReader bsr) {
			Armor = bsr.ReadByte();
			DamageTaken = bsr.ReadByte();
			BitsDamage = bsr.ReadSInt();
			bsr.ReadVector3(out VecFrom);
			/*VecFrom = new Vector3 { 		old engine?
				X = bsr.ReadCoord(),
				Y = bsr.ReadCoord(),
				Z = bsr.ReadCoord()
			};*/
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void AppendToWriter(IIndentedWriter iw) {
			iw.AppendLine($"armor: {Armor}");
			iw.AppendLine($"damage taken: {DamageTaken}");
			iw.AppendLine($"bits damage: {BitsDamage}");
			iw.Append($"vec from: {VecFrom:F3}");
		}
	}
}