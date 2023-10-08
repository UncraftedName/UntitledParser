using System;
using System.Numerics;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages.UserMessages {

	public class Damage : UserMessage {

		public byte Armor;
		public byte DamageTaken;
		// the actual damage bits are masked with Damage_GetShowOnHud()
		// in se2007 this is DMG_POISON | DMG_ACID | DMG_DROWN | DMG_BURN | DMG_SLOWBURN | DMG_NERVEGAS | DMG_RADIATION | DMG_SHOCK
		public DamageType VisibleBitsDamage;
		public Vector3 VecFrom;


		public Damage(SourceDemo? demoRef, byte value) : base(demoRef, value) {}


		protected override void Parse(ref BitStreamReader bsr) {
			Armor = bsr.ReadByte();
			DamageTaken = bsr.ReadByte();
			VisibleBitsDamage = (DamageType)bsr.ReadUInt();
			bsr.ReadVector3(out VecFrom);
			/*VecFrom = new Vector3 { 		old engine?
				X = bsr.ReadCoord(),
				Y = bsr.ReadCoord(),
				Z = bsr.ReadCoord()
			};*/
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.AppendLine($"armor: {Armor}");
			pw.AppendLine($"damage taken: {DamageTaken}");
			pw.AppendLine($"visible bits damage: ({(uint)VisibleBitsDamage}) {VisibleBitsDamage}");
			pw.Append($"vec from: {VecFrom:F3}");
		}


		[Flags]
		public enum DamageType : uint {
			DmgGeneric             = 0,
			DmgCrush               = 1 << 0,
			DmgBullet              = 1 << 1,
			DmgSlash               = 1 << 2,
			DmgBurn                = 1 << 3,
			DmgVehicle             = 1 << 4,
			DmgFall                = 1 << 5,
			DmgBlast               = 1 << 6,
			DmgClub                = 1 << 7,
			DmgShock               = 1 << 8,
			DmgSonic               = 1 << 9,
			DmgEnergyBeam          = 1 << 10,
			DmgPreventPhysicsForce = 1 << 11,
			DmgNeverGib            = 1 << 12,
			DmgAlwaysGib           = 1 << 13,
			DmgDrown               = 1 << 14,
			DmgParalyze            = 1 << 15,
			DmgNerveGas            = 1 << 16,
			DmgPoison              = 1 << 17,
			DmgRadiation           = 1 << 18,
			DmgDrownRecover        = 1 << 19,
			DmgAcid                = 1 << 20,
			DmgSlowBurn            = 1 << 21,
			DmgRemoveNoRagdoll     = 1 << 22,
			DmgPhysGun             = 1 << 23,
			DmgPlasma              = 1 << 24,
			DmgAirboat             = 1 << 25,
			DmgDissolve            = 1 << 26,
			DmgBlastSurface        = 1 << 27,
			DmgDirect              = 1 << 28,
			DmgBuckshot            = 1 << 29,
		}
	}
}
