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


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.AppendLine($"armor: {Armor}");
			pw.AppendLine($"damage taken: {DamageTaken}");
			pw.AppendLine($"bits damage: {BitsDamage}");
			pw.Append($"vec from: {VecFrom:F3}");
		}
	}
}
