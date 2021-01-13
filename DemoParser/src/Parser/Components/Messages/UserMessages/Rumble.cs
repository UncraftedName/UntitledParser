using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages.UserMessages {
	
	public class Rumble : UserMessage {

		public RumbleLookup RumbleType;
		public float Scale;
		public RumbleFlags RumbleFlags;
		

		public Rumble(SourceDemo? demoRef) : base(demoRef) {}


		protected override void Parse(ref BitStreamReader bsr) {
			RumbleType = (RumbleLookup)bsr.ReadSByte();
			Scale = bsr.ReadByte() / 100f;
			RumbleFlags = (RumbleFlags)bsr.ReadByte();
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void PrettyWrite(IPrettyWriter iw) {
			iw.AppendLine($"type: {RumbleType}");
			iw.AppendLine($"scale: {Scale}");
			iw.Append($"flags: {RumbleFlags}");
		}
	}


	public enum RumbleLookup : sbyte {
		RumbleInvalid = -1,
		RumbleStopAll = 0, // cease all current rumbling effects.

		// Weapons
		Pistol,
		Weap_357,
		Smg1,
		Ar2,
		ShotgunSingle,
		ShotgunDouble,
		Ar2AltFire,

		RpgMissile,
		CrowbarSwing,

		// Vehicles
		AirboatGun,
		JeepEngineLoop,
	
		FlatLeft,
		FlatRight,
		FlatBoth,

		// Damage
		DmgLow,
		DmgMed,
		DmgHigh,

		// Fall damage
		FallLong,
		FallShort,

		PhyscannonOpen,
		PhyscannonPunt,
		PhyscannonLow,
		PhyscannonMedium,
		PhyscannonHigh,

		PortalgunLeft,
		PortalgunRight,
		PortalPlacementFailure,

		NumRumbleEffects
	}


	[Flags]
	public enum RumbleFlags : byte {
		None            = 0,
		Stop            = 1,
		Loop            = 1 << 1,
		Restart         = 1 << 2,
		UpdateScale     = 1 << 3, // Apply DATA to this effect if already playing, but don't restart.   <-- DATA is scale * 100
		OnlyOne         = 1 << 4, // Don't play this effect if it is already playing.
		RandomAmplitude = 1 << 5, // Amplitude scale will be randomly chosen. Between 10% and 100%
		InitialScale    = 1 << 6  // Data is the initial scale to start this effect ( * 100 )
	}
}